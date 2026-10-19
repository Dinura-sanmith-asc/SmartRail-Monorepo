using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.Authorization;
using SmartRail.Application.Incidents.Categories;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Application.Incidents.References;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.Application.Incidents.UseCases;

public enum CreateIncidentOutcome { Created, Replayed, Invalid, Unauthorized, Forbidden, NotFound, Conflict, Unavailable }

public sealed record IncidentCreatedAssociation(Guid AssociationId, string Kind, Guid ResourceId, Guid? UpdateId, int? Revision);
public sealed record IncidentCreatedProjection(Guid IncidentId, string CategoryCode, string Description, string State,
    int Revision, string Source, DateTimeOffset ReportedAt, DateTimeOffset? OccurredAt,
    IReadOnlyList<IncidentCreatedAssociation> Associations);
public sealed record CreateIncidentResult(CreateIncidentOutcome Outcome, IncidentCreatedProjection? Incident = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
public sealed record CreateIncidentCommand(string? VerifiedSubject, string? IdempotencyKey, CreateIncidentInput? Input);

/// <summary>Creates only after current identity, scope, category and owner evidence have been verified.</summary>
public sealed class CreateIncidentHandler(
    IIncidentRepository repository,
    IIncidentUnitOfWork unitOfWork,
    IIncidentAccessEvaluator accessEvaluator,
    IIncidentCategoryCatalog categories,
    IFeature001IncidentReferenceVerifier feature001References,
    IFeature003IncidentReferenceVerifier feature003References,
    TimeProvider timeProvider)
{
    private const string Operation = "POST /api/incidents";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CreateIncidentResult> HandleAsync(CreateIncidentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var validation = CreateIncidentValidator.Validate(command.Input);
        if (!validation.IsValid) return new(CreateIncidentOutcome.Invalid, Errors: validation.Errors);
        if (string.IsNullOrWhiteSpace(command.VerifiedSubject)) return new(CreateIncidentOutcome.Unauthorized);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 200)
            return new(CreateIncidentOutcome.Invalid, Errors: new Dictionary<string, string[]> { ["Idempotency-Key"] = ["A non-empty idempotency key of at most 200 characters is required."] });

        var input = command.Input!;
        var requestedStationIds = input.Associations!
            .Where(x => x.Kind == IncidentReferenceKind.Station)
            .Select(x => x.ResourceId)
            .Distinct()
            .ToArray();
        var authorization = await accessEvaluator.AuthorizeAsync(command.VerifiedSubject, PermissionAction.CreateIncident,
            incidentId: null, requestedStationIds, isCreation: true, cancellationToken);
        if (authorization.Outcome == AuthorizationOutcome.Unavailable) return new(CreateIncidentOutcome.Unavailable);
        if (authorization.Outcome != AuthorizationOutcome.Allowed) return new(CreateIncidentOutcome.Forbidden);

        var key = command.IdempotencyKey.Trim();
        var payloadHash = HashCanonical(input);
        var lookup = new IdempotencyLookup(command.VerifiedSubject, Operation, key);
        // Reauthorize before looking up or returning a protected stored response.
        IncidentIdempotencyRecord? existing;
        try { existing = await repository.FindIdempotencyAsync(lookup, cancellationToken); }
        catch { return new(CreateIncidentOutcome.Unavailable); }
        if (existing is not null) return Replay(existing, payloadHash);

        var verified = new List<IncidentReference>(input.Associations!.Count);
        foreach (var reference in input.Associations)
        {
            IIncidentReferenceVerifier owner = reference.Kind is IncidentReferenceKind.Station or IncidentReferenceKind.Service
                ? feature001References
                : feature003References;
            IncidentReferenceCheck check;
            try
            {
                check = await owner.VerifyAsync(reference, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch
            {
                return new(CreateIncidentOutcome.Unavailable);
            }

            switch (check.Outcome)
            {
                case IncidentReferenceOutcome.Verified:
                    verified.Add(reference);
                    break;
                case IncidentReferenceOutcome.MissingOrInaccessible:
                    return new(CreateIncidentOutcome.NotFound);
                case IncidentReferenceOutcome.StaleOrInactive:
                    return new(CreateIncidentOutcome.Conflict);
                default:
                    return new(CreateIncidentOutcome.Unavailable);
            }
        }

        CategoryCheck category;
        try { category = await categories.CheckAsync(input.CategoryCode!, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(CreateIncidentOutcome.Unavailable); }
        if (category.Outcome == CategoryCheckOutcome.Unknown) return new(CreateIncidentOutcome.Invalid,
            Errors: new Dictionary<string, string[]> { ["categoryCode"] = ["The category code is not in the configured catalogue."] });
        if (category.Outcome != CategoryCheckOutcome.Approved) return new(CreateIncidentOutcome.Unavailable);

        var now = timeProvider.GetUtcNow();
        var incident = new Incident(Guid.NewGuid(), input.CategoryCode!, input.Description!, command.VerifiedSubject,
            now, input.OccurredAt?.ToUniversalTime());
        var associations = verified.Select(reference => new IncidentAssociation(Guid.NewGuid(), incident.IncidentId,
            ToDomainKind(reference.Kind), reference.ResourceId, now, 1, reference.UpdateId, reference.Revision)).ToArray();
        var evidence = JsonSerializer.Serialize(associations.Select(x => new { x.AssociationId, Kind = x.Kind.ToString(), x.ResourceId, x.DisruptionUpdateId, x.DisruptionRevision }));
        var revision = new IncidentRevision(incident.IncidentId, 1, incident.State, command.VerifiedSubject, now, evidence);
        var projection = ToProjection(incident, associations);
        var idempotency = new IncidentIdempotencyRecord(Guid.NewGuid(), command.VerifiedSubject, Operation, key,
            payloadHash, incident.IncidentId, CreatedStatusCode, JsonSerializer.Serialize(projection, JsonOptions), now);

        try
        {
            await unitOfWork.ExecuteAsync(async ct =>
            {
                await repository.CreateAsync(incident, revision, associations, idempotency, ct);
                return true;
            }, cancellationToken);
            return new(CreateIncidentOutcome.Created, projection);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            // Covers a simultaneous insert of the same actor/operation/key. The database's
            // unique constraint arbitrates; after rollback, return only a matching replay.
            try
            {
                existing = await repository.FindIdempotencyAsync(lookup, cancellationToken);
                if (existing is not null) return Replay(existing, payloadHash);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { }
            return new(CreateIncidentOutcome.Unavailable);
        }
    }

    private static CreateIncidentResult Replay(IncidentIdempotencyRecord record, string payloadHash)
    {
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(record.PayloadHash), Encoding.UTF8.GetBytes(payloadHash)))
            return new(CreateIncidentOutcome.Conflict);
        try
        {
            var projection = JsonSerializer.Deserialize<IncidentCreatedProjection>(record.ResponseJson, JsonOptions);
            return projection is null ? new(CreateIncidentOutcome.Unavailable) : new(CreateIncidentOutcome.Replayed, projection);
        }
        catch (JsonException) { return new(CreateIncidentOutcome.Unavailable); }
    }

    private static string HashCanonical(CreateIncidentInput input)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            categoryCode = input.CategoryCode!.Trim(),
            description = input.Description!.Trim(),
            occurredAt = input.OccurredAt?.ToUniversalTime(),
            associations = input.Associations!.OrderBy(x => x.Kind).ThenBy(x => x.ResourceId)
                .ThenBy(x => x.UpdateId).ThenBy(x => x.Revision)
                .Select(x => new { kind = x.Kind.ToString(), resourceId = x.ResourceId, updateId = x.UpdateId, revision = x.Revision })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static IncidentCreatedProjection ToProjection(Incident incident, IEnumerable<IncidentAssociation> associations) =>
        new(incident.IncidentId, incident.CategoryCode, incident.Description, "Reported", incident.Revision,
            "IncidentManagement", incident.ReportedAt, incident.OccurredAt,
            associations.Select(x => new IncidentCreatedAssociation(x.AssociationId, x.Kind.ToString(), x.ResourceId,
                x.DisruptionUpdateId, x.DisruptionRevision)).ToArray());

    private static IncidentAssociationKind ToDomainKind(IncidentReferenceKind kind) => kind switch
    {
        IncidentReferenceKind.Station => IncidentAssociationKind.Station,
        IncidentReferenceKind.Service => IncidentAssociationKind.Service,
        IncidentReferenceKind.Journey => IncidentAssociationKind.Journey,
        IncidentReferenceKind.Disruption => IncidentAssociationKind.Disruption,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private const int CreatedStatusCode = 201;
}
