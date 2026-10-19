using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.Authorization;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Application.Incidents.References;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.Application.Incidents.UseCases;

public enum UpdateIncidentOutcome { Updated, Replayed, Invalid, Unauthorized, Forbidden, NotFound, Conflict, Unavailable }

public sealed record UpdateIncidentInput(int? ExpectedRevision, IncidentState? State,
    IReadOnlyCollection<IncidentReference>? AddAssociations = null, IReadOnlyCollection<Guid>? RemoveAssociationIds = null);
public sealed record UpdateIncidentCommand(Guid IncidentId, string? VerifiedSubject, string? IdempotencyKey, UpdateIncidentInput? Input);
public sealed record UpdateIncidentResult(UpdateIncidentOutcome Outcome, IncidentCreatedProjection? Incident = null);

public static class UpdateIncidentInputValidator
{
    public static bool IsValid(UpdateIncidentInput? input)
    {
        if (input?.ExpectedRevision is null or <= 0) return false;
        if (input.State.HasValue && !Enum.IsDefined(input.State.Value)) return false;
        if (!input.State.HasValue && (input.AddAssociations?.Count ?? 0) == 0 && (input.RemoveAssociationIds?.Count ?? 0) == 0) return false;

        var additions = input.AddAssociations ?? [];
        var uniqueReferences = new HashSet<(IncidentReferenceKind Kind, Guid Id, Guid? UpdateId, int? Revision)>();
        foreach (var item in additions)
        {
            if (item is null || !Enum.IsDefined(item.Kind) || item.ResourceId == Guid.Empty) return false;
            if (item.Kind == IncidentReferenceKind.Disruption)
            {
                if (!item.UpdateId.HasValue || item.UpdateId.Value == Guid.Empty || item.Revision is null or <= 0) return false;
            }
            else if (item.UpdateId.HasValue || item.Revision.HasValue) return false;
            if (!uniqueReferences.Add((item.Kind, item.ResourceId, item.UpdateId, item.Revision))) return false;
        }

        var removalIds = input.RemoveAssociationIds ?? [];
        return removalIds.All(id => id != Guid.Empty) && removalIds.Distinct().Count() == removalIds.Count;
    }
}

/// <summary>Applies authorized lifecycle and association changes as one revisioned mutation.</summary>
public sealed class UpdateIncidentHandler(
    IIncidentRepository repository,
    IIncidentUnitOfWork unitOfWork,
    IIncidentAccessEvaluator accessEvaluator,
    IFeature001IncidentReferenceVerifier feature001References,
    IFeature003IncidentReferenceVerifier feature003References,
    TimeProvider timeProvider)
{
    private const string OperationPrefix = "PATCH /api/incidents/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UpdateIncidentResult> HandleAsync(UpdateIncidentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.IncidentId == Guid.Empty || !UpdateIncidentInputValidator.IsValid(command.Input) ||
            string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 200)
            return new(UpdateIncidentOutcome.Invalid);
        if (string.IsNullOrWhiteSpace(command.VerifiedSubject)) return new(UpdateIncidentOutcome.Unauthorized);

        var input = command.Input!;
        IncidentRecord? current;
        try { current = await repository.FindAsync(command.IncidentId, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(UpdateIncidentOutcome.Unavailable); }
        if (current is null) return new(UpdateIncidentOutcome.NotFound);

        var removalIds = (input.RemoveAssociationIds ?? []).ToHashSet();
        var activeById = current.Associations.Where(x => x.IsActive).ToDictionary(x => x.AssociationId);

        // Require scope for every currently linked station and every station being
        // introduced; removal never bypasses authority over the existing resource.
        var targetStationIds = current.Associations
            .Where(x => x.IsActive && x.Kind == IncidentAssociationKind.Station)
            .Select(x => x.ResourceId)
            .Concat((input.AddAssociations ?? []).Where(x => x.Kind == IncidentReferenceKind.Station).Select(x => x.ResourceId))
            .Distinct().ToArray();
        AuthorizationDecision authorization;
        try
        {
            authorization = await accessEvaluator.AuthorizeAsync(command.VerifiedSubject,
                PermissionAction.UpdateIncidentLifecycle, current.Incident.IncidentId, targetStationIds,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(UpdateIncidentOutcome.Unavailable); }
        if (authorization.Outcome == AuthorizationOutcome.Unavailable) return new(UpdateIncidentOutcome.Unavailable);
        if (authorization.Outcome != AuthorizationOutcome.Allowed) return new(UpdateIncidentOutcome.Forbidden);

        var key = command.IdempotencyKey.Trim();
        var operation = OperationPrefix + command.IncidentId.ToString("D");
        var lookup = new IdempotencyLookup(command.VerifiedSubject, operation, key);
        var requestHash = HashRequest(command.IncidentId, input);
        IncidentIdempotencyRecord? previous;
        try { previous = await repository.FindIdempotencyAsync(lookup, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(UpdateIncidentOutcome.Unavailable); }
        if (previous is not null) return Replay(previous, requestHash);

        var incident = current.Incident;
        if (input.ExpectedRevision!.Value != incident.Revision) return new(UpdateIncidentOutcome.Conflict);
        if (removalIds.Any(id => !activeById.ContainsKey(id))) return new(UpdateIncidentOutcome.NotFound);
        var activeReferences = current.Associations.Where(x => x.IsActive && !removalIds.Contains(x.AssociationId)).ToList();
        var requested = input.AddAssociations ?? [];
        foreach (var reference in requested)
        {
            var replacedWithinRequest = current.Associations.Any(x => removalIds.Contains(x.AssociationId) &&
                x.Kind == ToDomainKind(reference.Kind) && x.ResourceId == reference.ResourceId &&
                (reference.Kind != IncidentReferenceKind.Disruption ||
                 x.DisruptionUpdateId == reference.UpdateId && x.DisruptionRevision == reference.Revision));
            if (replacedWithinRequest) return new(UpdateIncidentOutcome.Invalid);

            var duplicateActive = activeReferences.Any(x => x.Kind == ToDomainKind(reference.Kind) && x.ResourceId == reference.ResourceId &&
                (reference.Kind != IncidentReferenceKind.Disruption ||
                 x.DisruptionUpdateId == reference.UpdateId && x.DisruptionRevision == reference.Revision));
            if (duplicateActive) return new(UpdateIncidentOutcome.Invalid);

            IIncidentReferenceVerifier owner = reference.Kind is IncidentReferenceKind.Station or IncidentReferenceKind.Service
                ? feature001References
                : feature003References;
            IncidentReferenceCheck check;
            try { check = await owner.VerifyAsync(reference, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { return new(UpdateIncidentOutcome.Unavailable); }
            switch (check.Outcome)
            {
                case IncidentReferenceOutcome.Verified: break;
                case IncidentReferenceOutcome.MissingOrInaccessible: return new(UpdateIncidentOutcome.NotFound);
                case IncidentReferenceOutcome.StaleOrInactive: return new(UpdateIncidentOutcome.Conflict);
                default: return new(UpdateIncidentOutcome.Unavailable);
            }
        }

        var hasAnchorAfterMutation = activeReferences.Any(x => x.Kind is IncidentAssociationKind.Station or IncidentAssociationKind.Service) ||
            requested.Any(x => x.Kind is IncidentReferenceKind.Station or IncidentReferenceKind.Service);
        if (!hasAnchorAfterMutation) return new(UpdateIncidentOutcome.Invalid);

        var priorState = incident.State;
        var now = timeProvider.GetUtcNow();
        try
        {
            if (input.State.HasValue)
            {
                if (input.State.Value == incident.State) return new(UpdateIncidentOutcome.Invalid);
                incident.TransitionTo(input.State.Value, input.ExpectedRevision.Value, now);
            }
            else
            {
                incident.AdvanceRevisionForAssociationChange(input.ExpectedRevision.Value, now);
            }
        }
        catch (InvalidOperationException) { return new(UpdateIncidentOutcome.Invalid); }
        catch (ArgumentException) { return new(UpdateIncidentOutcome.Invalid); }

        var nextRevision = incident.Revision;
        var removedAssociations = removalIds.Select(id => activeById[id]).ToArray();
        foreach (var association in removedAssociations) association.Remove(input.ExpectedRevision.Value, nextRevision, now);
        var addedAssociations = requested.Select(reference => new IncidentAssociation(Guid.NewGuid(), incident.IncidentId,
            ToDomainKind(reference.Kind), reference.ResourceId, now, nextRevision, reference.UpdateId, reference.Revision)).ToArray();
        var resultingAssociations = activeReferences.Concat(addedAssociations).ToArray();
        var evidence = JsonSerializer.Serialize(new
        {
            changes = new
            {
                added = addedAssociations.Select(Evidence),
                removed = removedAssociations.Select(Evidence)
            },
            active = resultingAssociations.Select(Evidence)
        });
        IncidentState? previousState = input.State.HasValue ? priorState : null;
        var revision = new IncidentRevision(incident.IncidentId, nextRevision, incident.State,
            command.VerifiedSubject, now, evidence, previousState);
        var projection = Project(incident, resultingAssociations);
        var idempotency = new IncidentIdempotencyRecord(Guid.NewGuid(), command.VerifiedSubject, operation,
            key, requestHash, incident.IncidentId, 200, JsonSerializer.Serialize(projection, JsonOptions), now);

        try
        {
            var persisted = await unitOfWork.ExecuteAsync(ct => repository.UpdateAsync(incident, revision,
                addedAssociations, removedAssociations, idempotency, input.ExpectedRevision.Value, ct), cancellationToken);
            return persisted ? new(UpdateIncidentOutcome.Updated, projection) : new(UpdateIncidentOutcome.Conflict);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (IncidentConcurrencyException)
        {
            try
            {
                previous = await repository.FindIdempotencyAsync(lookup, cancellationToken);
                if (previous is not null) return Replay(previous, requestHash);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { return new(UpdateIncidentOutcome.Unavailable); }
            return new(UpdateIncidentOutcome.Conflict);
        }
        catch
        {
            try
            {
                previous = await repository.FindIdempotencyAsync(lookup, cancellationToken);
                if (previous is not null) return Replay(previous, requestHash);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { }
            return new(UpdateIncidentOutcome.Unavailable);
        }
    }

    private static object Evidence(IncidentAssociation x) => new
    {
        x.AssociationId, Kind = x.Kind.ToString(), x.ResourceId,
        x.DisruptionUpdateId, x.DisruptionRevision, x.AddedRevision, x.RemovedRevision
    };

    private static IncidentAssociationKind ToDomainKind(IncidentReferenceKind kind) => kind switch
    {
        IncidentReferenceKind.Station => IncidentAssociationKind.Station,
        IncidentReferenceKind.Service => IncidentAssociationKind.Service,
        IncidentReferenceKind.Journey => IncidentAssociationKind.Journey,
        IncidentReferenceKind.Disruption => IncidentAssociationKind.Disruption,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static UpdateIncidentResult Replay(IncidentIdempotencyRecord record, string hash)
    {
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(record.PayloadHash), Encoding.UTF8.GetBytes(hash)))
            return new(UpdateIncidentOutcome.Conflict);
        try
        {
            var response = JsonSerializer.Deserialize<IncidentCreatedProjection>(record.ResponseJson, JsonOptions);
            return response is null ? new(UpdateIncidentOutcome.Unavailable) : new(UpdateIncidentOutcome.Replayed, response);
        }
        catch (JsonException) { return new(UpdateIncidentOutcome.Unavailable); }
    }

    private static string HashRequest(Guid incidentId, UpdateIncidentInput input)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            incidentId,
            expectedRevision = input.ExpectedRevision,
            state = input.State?.ToString(),
            addAssociations = (input.AddAssociations ?? []).OrderBy(x => x.Kind).ThenBy(x => x.ResourceId)
                .ThenBy(x => x.UpdateId).ThenBy(x => x.Revision)
                .Select(x => new { kind = x.Kind.ToString(), resourceId = x.ResourceId, updateId = x.UpdateId, revision = x.Revision }),
            removeAssociationIds = (input.RemoveAssociationIds ?? []).OrderBy(x => x)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static IncidentCreatedProjection Project(Incident incident, IReadOnlyCollection<IncidentAssociation> associations) =>
        new(incident.IncidentId, incident.CategoryCode, incident.Description, incident.State.ToString(),
            incident.Revision, "IncidentManagement", incident.ReportedAt, incident.OccurredAt,
            associations.Where(x => x.IsActive).Select(x => new IncidentCreatedAssociation(x.AssociationId,
                x.Kind.ToString(), x.ResourceId, x.DisruptionUpdateId, x.DisruptionRevision)).ToArray());
}
