using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Incidents.Authorization;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Domain.Identity;

namespace SmartRail.Application.Incidents.UseCases;

public enum ListIncidentsOutcome { Listed, Unauthorized, Forbidden, Unavailable }
public sealed record ListIncidentsQuery(string? VerifiedSubject, IncidentFilters Filters);
public sealed record IncidentListItem(Guid IncidentId, string CategoryCode, string Description, string State,
    int Revision, string Source, DateTimeOffset ReportedAt, DateTimeOffset? OccurredAt,
    IReadOnlyList<IncidentCreatedAssociation> Associations);
public sealed record ListIncidentsResult(ListIncidentsOutcome Outcome, IReadOnlyList<IncidentListItem> Incidents);

/// <summary>Returns only incidents authorized by current role and per-resource station evidence.</summary>
public sealed class ListIncidentsHandler(
    IIncidentRepository repository,
    ResolveCurrentRolesHandler roles,
    IIncidentAccessEvaluator accessEvaluator)
{
    public async Task<ListIncidentsResult> HandleAsync(ListIncidentsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.VerifiedSubject))
            return new(ListIncidentsOutcome.Unauthorized, []);

        CurrentRolesResult resolved;
        try { resolved = await roles.HandleAsync(new(new VerifiedPrincipal(query.VerifiedSubject)), cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(ListIncidentsOutcome.Unavailable, []); }
        if (resolved.Outcome == RoleResolutionOutcome.Unavailable)
            return new(ListIncidentsOutcome.Unavailable, []);
        if (!RolePermissionCatalog.IsGranted(resolved.Roles, PermissionAction.ViewIncidents))
            return new(ListIncidentsOutcome.Forbidden, []);

        IReadOnlyList<IncidentRecord> records;
        try { records = await repository.ListAsync(query.Filters, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(ListIncidentsOutcome.Unavailable, []); }

        var permitted = new List<IncidentListItem>(records.Count);
        foreach (var record in records)
        {
            var stationIds = record.Associations
                .Where(item => item.Kind == SmartRail.Domain.Incidents.IncidentAssociationKind.Station)
                .Select(item => item.ResourceId)
                .Distinct()
                .ToArray();
            AuthorizationDecision decision;
            try
            {
                decision = await accessEvaluator.AuthorizeAsync(query.VerifiedSubject, PermissionAction.ViewIncidents,
                    record.Incident.IncidentId, stationIds, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { return new(ListIncidentsOutcome.Unavailable, []); }
            if (decision.Outcome == AuthorizationOutcome.Unavailable)
                return new(ListIncidentsOutcome.Unavailable, []);
            if (decision.Outcome != AuthorizationOutcome.Allowed) continue;

            permitted.Add(new IncidentListItem(record.Incident.IncidentId, record.Incident.CategoryCode,
                record.Incident.Description, record.Incident.State.ToString(), record.Incident.Revision,
                "IncidentManagement", record.Incident.ReportedAt, record.Incident.OccurredAt,
                record.Associations.Select(item => new IncidentCreatedAssociation(item.AssociationId,
                    item.Kind.ToString(), item.ResourceId, item.DisruptionUpdateId, item.DisruptionRevision)).ToArray()));
        }
        return new(ListIncidentsOutcome.Listed, permitted);
    }
}
