using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Application.Incidents.Authorization;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Application.Incidents.UseCases;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.UnitTests.Incidents;

public sealed class ListIncidentsHandlerTests
{
    [Fact]
    public async Task Controller_with_current_view_grant_receives_safe_incident_projection()
    {
        var repository = new MemoryIncidentRepository([Record([Guid.NewGuid()])]);
        var handler = Create(repository, [RoleCode.Controller]);

        var result = await handler.HandleAsync(new("controller-sub", new()));

        Assert.Equal(ListIncidentsOutcome.Listed, result.Outcome);
        var incident = Assert.Single(result.Incidents);
        Assert.Equal("IncidentManagement", incident.Source);
        Assert.Single(incident.Associations);
        Assert.DoesNotContain("private-reporter", System.Text.Json.JsonSerializer.Serialize(incident));
    }

    [Fact]
    public async Task Candidate_outside_any_station_scope_is_filtered_without_existence_disclosure()
    {
        var repository = new MemoryIncidentRepository([Record([Guid.NewGuid(), Guid.NewGuid()])]);
        var access = new RecordingAccess { Decision = AuthorizationDecision.Denied("out-of-scope") };
        var handler = Create(repository, [RoleCode.StationStaff], access);

        var result = await handler.HandleAsync(new("staff-sub", new()));

        Assert.Equal(ListIncidentsOutcome.Listed, result.Outcome);
        Assert.Empty(result.Incidents);
        Assert.Equal(2, access.StationIds.Single().Count);
    }

    [Fact]
    public async Task Authorized_empty_list_is_distinct_from_missing_or_unavailable_role_mapping()
    {
        var emptyList = await Create(new MemoryIncidentRepository([]), [RoleCode.StationStaff])
            .HandleAsync(new("staff-sub", new()));
        var noGrant = await Create(new MemoryIncidentRepository([]), [])
            .HandleAsync(new("unassigned-sub", new()));
        var unavailable = await Create(new MemoryIncidentRepository([]), [], unavailable: true)
            .HandleAsync(new("unknown-sub", new()));

        Assert.Equal(ListIncidentsOutcome.Listed, emptyList.Outcome);
        Assert.Empty(emptyList.Incidents);
        Assert.Equal(ListIncidentsOutcome.Forbidden, noGrant.Outcome);
        Assert.Equal(ListIncidentsOutcome.Unavailable, unavailable.Outcome);
    }

    private static ListIncidentsHandler Create(MemoryIncidentRepository repository, IReadOnlyList<RoleCode> roles,
        RecordingAccess? access = null, bool unavailable = false) => new(repository,
        new ResolveCurrentRolesHandler(new RolesRepository(roles, unavailable)), access ?? new RecordingAccess());

    private static IncidentRecord Record(IReadOnlyList<Guid> stationIds)
    {
        var now = new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
        var incident = new Incident(Guid.NewGuid(), "TEST-CATEGORY", "Track obstruction", "private-reporter", now);
        var associations = stationIds.Select(station => new IncidentAssociation(Guid.NewGuid(), incident.IncidentId,
            IncidentAssociationKind.Station, station, now, 1)).ToArray();
        return new(incident, associations);
    }

    private sealed class RecordingAccess : IIncidentAccessEvaluator
    {
        public AuthorizationDecision Decision { get; set; } = AuthorizationDecision.Allowed();
        public List<IReadOnlyCollection<Guid>> StationIds { get; } = [];
        public Task<AuthorizationDecision> AuthorizeAsync(string verifiedSubject, PermissionAction action,
            Guid? incidentId, IReadOnlyCollection<Guid> associatedStationIds, bool isCreation = false,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal("ViewIncidents", action.ToString());
            Assert.False(isCreation);
            StationIds.Add(associatedStationIds);
            return Task.FromResult(Decision);
        }
    }

    private sealed class RolesRepository(IReadOnlyList<RoleCode> roles, bool unavailable) : IRoleAssignmentRepository
    {
        public Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) =>
            unavailable ? throw new InvalidOperationException("unavailable") : Task.FromResult(roles);
        public Task<IReadOnlyList<RoleAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RoleAssignment>>([]);
        public Task GrantAsync(RoleAssignment assignment, RoleAssignmentAudit audit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class MemoryIncidentRepository(IReadOnlyList<IncidentRecord> records) : IIncidentRepository
    {
        public Task<IncidentRecord?> FindAsync(Guid incidentId, CancellationToken cancellationToken = default) => Task.FromResult<IncidentRecord?>(null);
        public Task<IReadOnlyList<IncidentRecord>> ListAsync(IncidentFilters filters, CancellationToken cancellationToken = default) => Task.FromResult(records);
        public Task<IncidentIdempotencyRecord?> FindIdempotencyAsync(IdempotencyLookup lookup, CancellationToken cancellationToken = default) => Task.FromResult<IncidentIdempotencyRecord?>(null);
        public Task CreateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> associations, IncidentIdempotencyRecord idempotencyRecord, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Incident incident, IncidentRevision revision, IReadOnlyCollection<IncidentAssociation> addAssociations, IReadOnlyCollection<IncidentAssociation> removeAssociations, IncidentIdempotencyRecord idempotencyRecord, int expectedRevision, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
