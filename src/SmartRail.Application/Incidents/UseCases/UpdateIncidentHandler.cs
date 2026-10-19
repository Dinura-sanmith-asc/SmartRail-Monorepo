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

/// <summary>Applies authorized, forward-only incident lifecycle changes with optimistic concurrency.</summary>
public sealed class UpdateIncidentHandler(
    IIncidentRepository repository,
    IIncidentUnitOfWork unitOfWork,
    IIncidentAccessEvaluator accessEvaluator,
    TimeProvider timeProvider)
{
    private const string OperationPrefix = "PATCH /api/incidents/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UpdateIncidentResult> HandleAsync(UpdateIncidentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.IncidentId == Guid.Empty || command.Input?.ExpectedRevision is null or <= 0 ||
            command.Input.State is null ||
            command.Input.AddAssociations?.Count > 0 || command.Input.RemoveAssociationIds?.Count > 0 ||
            string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 200)
            return new(UpdateIncidentOutcome.Invalid);
        if (string.IsNullOrWhiteSpace(command.VerifiedSubject)) return new(UpdateIncidentOutcome.Unauthorized);

        IncidentRecord? current;
        try { current = await repository.FindAsync(command.IncidentId, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(UpdateIncidentOutcome.Unavailable); }
        if (current is null) return new(UpdateIncidentOutcome.NotFound);

        var stationIds = current.Associations
            .Where(x => x.Kind == IncidentAssociationKind.Station)
            .Select(x => x.ResourceId).Distinct().ToArray();
        AuthorizationDecision authorization;
        try
        {
            authorization = await accessEvaluator.AuthorizeAsync(command.VerifiedSubject,
                PermissionAction.UpdateIncidentLifecycle, current.Incident.IncidentId, stationIds,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(UpdateIncidentOutcome.Unavailable); }
        if (authorization.Outcome == AuthorizationOutcome.Unavailable) return new(UpdateIncidentOutcome.Unavailable);
        if (authorization.Outcome != AuthorizationOutcome.Allowed) return new(UpdateIncidentOutcome.Forbidden);

        var key = command.IdempotencyKey.Trim();
        var operation = OperationPrefix + command.IncidentId.ToString("D");
        var lookup = new IdempotencyLookup(command.VerifiedSubject, operation, key);
        var requestHash = HashRequest(command.IncidentId, command.Input);
        IncidentIdempotencyRecord? previous;
        try { previous = await repository.FindIdempotencyAsync(lookup, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(UpdateIncidentOutcome.Unavailable); }
        if (previous is not null) return Replay(previous, requestHash);

        var incident = current.Incident;
        if (command.Input.ExpectedRevision.Value != incident.Revision) return new(UpdateIncidentOutcome.Conflict);
        if (command.Input.State.Value == incident.State) return new(UpdateIncidentOutcome.Invalid);
        var priorState = incident.State;
        try { incident.TransitionTo(command.Input.State.Value, command.Input.ExpectedRevision.Value, timeProvider.GetUtcNow()); }
        catch (InvalidOperationException) { return new(UpdateIncidentOutcome.Invalid); }
        catch (ArgumentException) { return new(UpdateIncidentOutcome.Invalid); }

        var now = incident.UpdatedAt;
        var evidence = JsonSerializer.Serialize(current.Associations.Select(x => new
        {
            x.AssociationId, Kind = x.Kind.ToString(), x.ResourceId,
            x.DisruptionUpdateId, x.DisruptionRevision
        }));
        var revision = new IncidentRevision(incident.IncidentId, incident.Revision, incident.State,
            command.VerifiedSubject, now, evidence, previousState: priorState);
        var projection = Project(incident, current.Associations);
        var idempotency = new IncidentIdempotencyRecord(Guid.NewGuid(), command.VerifiedSubject, operation,
            key, requestHash, incident.IncidentId, 200, JsonSerializer.Serialize(projection, JsonOptions), now);

        try
        {
            var persisted = await unitOfWork.ExecuteAsync(ct => repository.UpdateAsync(incident, revision,
                Array.Empty<IncidentAssociation>(), Array.Empty<IncidentAssociation>(), idempotency,
                command.Input.ExpectedRevision.Value, ct), cancellationToken);
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
            state = input.State?.ToString()
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static IncidentCreatedProjection Project(Incident incident, IReadOnlyCollection<IncidentAssociation> associations) =>
        new(incident.IncidentId, incident.CategoryCode, incident.Description, incident.State.ToString(),
            incident.Revision, "IncidentManagement", incident.ReportedAt, incident.OccurredAt,
            associations.Where(x => x.IsActive).Select(x => new IncidentCreatedAssociation(x.AssociationId,
                x.Kind.ToString(), x.ResourceId, x.DisruptionUpdateId, x.DisruptionRevision)).ToArray());
}
