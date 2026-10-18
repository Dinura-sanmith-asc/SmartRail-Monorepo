using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Application.Identity.Queries;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;

namespace SmartRail.UnitTests.Identity;

public sealed class AuthorizationHandlerTests
{
    private static readonly VerifiedPrincipal Principal = new("verified|opaque-subject");

    [Fact]
    public async Task Role_resolution_distinguishes_verified_empty_unavailable_and_observes_revocation_next_call()
    {
        var repository = new FakeRoleRepository { Roles = [RoleCode.Driver] };
        var resolver = new ResolveCurrentRolesHandler(repository);
        Assert.Equal([RoleCode.Driver], (await resolver.HandleAsync(new(Principal))).Roles);
        repository.Roles = [];
        Assert.Empty((await resolver.HandleAsync(new(Principal))).Roles);
        repository.FailReads = true;
        Assert.Equal(RoleResolutionOutcome.Unavailable, (await resolver.HandleAsync(new(Principal))).Outcome);

        repository.FailReads = false;
        var identity = await new GetCurrentIdentityHandler(resolver).HandleAsync(new(Principal));
        Assert.Equal(CurrentIdentityOutcome.Verified, identity.Outcome);
        Assert.Equal(Principal.CognitoSubject, identity.Subject);
        Assert.Empty(identity.Roles);
    }

    [Fact]
    public async Task Driver_journey_access_requires_current_assignment()
    {
        var driver = NewAuthorizer([RoleCode.Driver], out var evidence);
        evidence.Driver = EvidenceOutcome.VerifiedMatch;
        Assert.Equal(AuthorizationOutcome.Allowed, (await driver.HandleAsync(new(Principal,
            PermissionAction.SubmitDriverProgressObservation, ProtectedResource.Journey(Guid.NewGuid())))).Outcome);

        evidence.Driver = EvidenceOutcome.VerifiedNoMatch;
        Assert.Equal(AuthorizationOutcome.Denied, (await driver.HandleAsync(new(Principal,
            PermissionAction.ViewDriverAssignments, ProtectedResource.Journey(Guid.NewGuid())))).Outcome);
        evidence.Driver = EvidenceOutcome.Stale;
        Assert.Equal(AuthorizationOutcome.Denied, (await driver.HandleAsync(new(Principal,
            PermissionAction.ViewDriverAssignments, ProtectedResource.Journey(Guid.NewGuid())))).Outcome);
        evidence.Driver = EvidenceOutcome.Unavailable;
        Assert.Equal(AuthorizationOutcome.Unavailable, (await driver.HandleAsync(new(Principal,
            PermissionAction.ViewDriverAssignments, ProtectedResource.Journey(Guid.NewGuid())))).Outcome);
    }

    [Fact]
    public async Task Passenger_order_access_requires_current_owner_match()
    {
        var passenger = NewAuthorizer([RoleCode.Passenger], out var evidence);
        evidence.Order = EvidenceOutcome.VerifiedMatch;
        Assert.Equal(AuthorizationOutcome.Allowed, (await passenger.HandleAsync(new(Principal,
            PermissionAction.ViewOwnCateringOrder, ProtectedResource.CateringOrder(Guid.NewGuid())))).Outcome);
        evidence.Order = EvidenceOutcome.VerifiedNoMatch;
        Assert.Equal(AuthorizationOutcome.Denied, (await passenger.HandleAsync(new(Principal,
            PermissionAction.ViewOwnCateringOrder, ProtectedResource.CateringOrder(Guid.NewGuid())))).Outcome);
        evidence.Order = EvidenceOutcome.Unavailable;
        Assert.Equal(AuthorizationOutcome.Unavailable, (await passenger.HandleAsync(new(Principal,
            PermissionAction.ViewOwnCateringOrder, ProtectedResource.CateringOrder(Guid.NewGuid())))).Outcome);
    }

    [Fact]
    public async Task Station_staff_requires_current_station_scope_but_controller_grant_can_satisfy_union()
    {
        var staff = NewAuthorizer([RoleCode.StationStaff], out var evidence);
        evidence.StationIds.Add(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var target = ProtectedResource.Station(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        Assert.Equal(AuthorizationOutcome.Allowed, (await staff.HandleAsync(new(Principal, PermissionAction.ViewOperationsDashboard, target))).Outcome);
        Assert.Equal(AuthorizationOutcome.Denied, (await staff.HandleAsync(new(Principal, PermissionAction.ViewOperationsDashboard,
            ProtectedResource.Station(Guid.NewGuid())))).Outcome);

        var mixed = NewAuthorizer([RoleCode.StationStaff, RoleCode.Controller], out var mixedEvidence);
        mixedEvidence.Incident = new(EvidenceOutcome.VerifiedMatch, Guid.NewGuid());
        var decision = await mixed.HandleAsync(new(Principal, PermissionAction.ViewIncidents, ProtectedResource.Incident(Guid.NewGuid())));
        Assert.Equal(AuthorizationOutcome.Allowed, decision.Outcome);
    }

    [Fact]
    public async Task Incident_and_scope_sources_fail_closed_for_missing_stale_revoked_and_unavailable_evidence()
    {
        var authorizer = NewAuthorizer([RoleCode.StationStaff], out var evidence);
        var incident = ProtectedResource.Incident(Guid.NewGuid());
        evidence.Incident = new(EvidenceOutcome.Missing);
        Assert.Equal(AuthorizationOutcome.Denied, (await authorizer.HandleAsync(new(Principal, PermissionAction.UpdateIncidentLifecycle, incident))).Outcome);
        evidence.Incident = new(EvidenceOutcome.Unavailable);
        Assert.Equal(AuthorizationOutcome.Unavailable, (await authorizer.HandleAsync(new(Principal, PermissionAction.UpdateIncidentLifecycle, incident))).Outcome);
        evidence.Incident = new(EvidenceOutcome.VerifiedMatch, Guid.NewGuid());
        Assert.Equal(AuthorizationOutcome.Denied, (await authorizer.HandleAsync(new(Principal, PermissionAction.UpdateIncidentLifecycle, incident))).Outcome);
        evidence.StationIds.Add(evidence.Incident.StationId!.Value);
        Assert.Equal(AuthorizationOutcome.Allowed, (await authorizer.HandleAsync(new(Principal, PermissionAction.UpdateIncidentLifecycle, incident))).Outcome);
    }

    [Fact]
    public async Task Controller_only_actions_are_denied_to_staff_and_allowed_to_controller_without_driver_scope()
    {
        var staff = NewAuthorizer([RoleCode.StationStaff], out _);
        var controller = NewAuthorizer([RoleCode.Controller], out _);
        var request = new AuthorizationRequest(Principal, PermissionAction.ConfirmJourneyCancellation, ProtectedResource.Journey(Guid.NewGuid()));
        Assert.Equal(AuthorizationOutcome.Denied, (await staff.HandleAsync(request)).Outcome);
        Assert.Equal(AuthorizationOutcome.Allowed, (await controller.HandleAsync(request)).Outcome);
    }

    [Fact]
    public async Task Invalid_or_missing_resource_context_never_grants_a_scoped_action()
    {
        var driver = NewAuthorizer([RoleCode.Driver], out _);
        Assert.Equal(AuthorizationOutcome.Denied, (await driver.HandleAsync(new(Principal,
            PermissionAction.SubmitDriverProgressObservation, ProtectedResource.None))).Outcome);
        Assert.Equal(AuthorizationOutcome.Denied, (await driver.HandleAsync(new(Principal,
            PermissionAction.SubmitDriverProgressObservation, ProtectedResource.Journey(Guid.Empty)))).Outcome);
    }

    private static AuthorizeOperationHandler NewAuthorizer(RoleCode[] roles, out FakeEvidence evidence)
    {
        var roleResolver = new ResolveCurrentRolesHandler(new FakeRoleRepository { Roles = roles });
        evidence = new FakeEvidence();
        var scope = new ResolveResourceScopeHandler(new FakeStationScopeRepository(evidence.StationIds), evidence, evidence, evidence);
        return new(roleResolver, scope);
    }

    private sealed class FakeRoleRepository : IRoleAssignmentRepository
    {
        public IReadOnlyList<RoleCode> Roles { get; set; } = [];
        public bool FailReads { get; set; }
        public Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) =>
            FailReads ? Task.FromException<IReadOnlyList<RoleCode>>(new InvalidOperationException()) : Task.FromResult(Roles);
        public Task<IReadOnlyList<RoleAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RoleAssignment>>([]);
        public Task GrantAsync(RoleAssignment assignment, RoleAssignmentAudit audit, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class FakeStationScopeRepository(HashSet<Guid> stationIds) : IStationScopeRepository
    {
        public Task<IReadOnlyList<Guid>> GetActiveStationIdsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(stationIds.ToArray());
        public Task<IReadOnlyList<StationScopeAssignment>> GetActiveAssignmentsAsync(string verifiedCognitoSubject, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<StationScopeAssignment>>([]);
        public Task GrantAsync(StationScopeAssignment assignment, StationScopeAssignmentAudit audit, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> RevokeAsync(Guid assignmentId, string verifiedActorSubject, DateTimeOffset occurredAtUtc, Guid auditId, string? evidenceReference = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class FakeEvidence : IDriverJourneyAssignmentEvidence, IIncidentResourceEvidence, IPassengerOrderOwnershipEvidence
    {
        public EvidenceOutcome Driver { get; set; } = EvidenceOutcome.Missing;
        public EvidenceOutcome Order { get; set; } = EvidenceOutcome.Missing;
        public IncidentResourceEvidence Incident { get; set; } = new(EvidenceOutcome.Missing);
        public HashSet<Guid> StationIds { get; } = [];
        public Task<EvidenceCheck> CheckAssignmentAsync(string verifiedDriverSubject, Guid journeyId, CancellationToken cancellationToken = default) => Task.FromResult(new EvidenceCheck(Driver));
        public Task<IncidentResourceEvidence> ResolveIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default) => Task.FromResult(Incident);
        public Task<EvidenceCheck> CheckOwnershipAsync(string verifiedPassengerSubject, Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult(new EvidenceCheck(Order));
    }
}
