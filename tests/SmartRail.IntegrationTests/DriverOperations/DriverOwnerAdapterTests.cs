using SmartRail.Application.DriverOperations;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Domain.DriverOperations.Entities;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Infrastructure.DriverOperations;

namespace SmartRail.IntegrationTests.DriverOperations;

public sealed class DriverOwnerAdapterTests
{
    private static readonly DateTimeOffset At = new(2026,10,20,8,0,0,TimeSpan.Zero);

    [Fact]
    public async Task AssignmentEvidenceUsesCurrentExactStoredOwnershipAndFailsClosed()
    {
        var repository = new DriverRepository();
        var adapter = new PostgreSqlDriverJourneyAssignmentEvidence(repository);
        Assert.Equal(EvidenceOutcome.VerifiedMatch, (await adapter.CheckAssignmentAsync("driver", repository.Assignment.JourneyInstanceId)).Outcome);
        Assert.Equal(EvidenceOutcome.VerifiedNoMatch, (await adapter.CheckAssignmentAsync("other", repository.Assignment.JourneyInstanceId)).Outcome);
        Assert.Equal(EvidenceOutcome.Missing, (await adapter.CheckAssignmentAsync("driver", Guid.Empty)).Outcome);
        repository.Assignment.Revoke("operator", At.AddMinutes(1));
        Assert.Equal(EvidenceOutcome.VerifiedNoMatch, (await adapter.CheckAssignmentAsync("driver", repository.Assignment.JourneyInstanceId)).Outcome);
        repository.Unavailable = true;
        Assert.Equal(EvidenceOutcome.Unavailable, (await adapter.CheckAssignmentAsync("driver", repository.Assignment.JourneyInstanceId)).Outcome);
    }

    [Fact]
    public async Task GenuineObservationRequiresCurrentReviewGrantSameJourneyAndActualRevision()
    {
        var repository = new DriverRepository();
        var roles = new Roles();
        var history = new Journeys(repository.Assignment.JourneyInstanceId);
        var adapter = new StoredDriverObservationVerifier(repository, new(roles), history);
        Assert.Equal(DriverObservationVerificationOutcome.Verified, (await adapter.VerifyAsync("controller", repository.Observation.ObservationId, repository.Assignment.JourneyInstanceId)).Outcome);
        roles.Active = [RoleCode.Driver];
        Assert.Equal(DriverObservationVerificationOutcome.MissingOrInaccessible, (await adapter.VerifyAsync("driver", repository.Observation.ObservationId, repository.Assignment.JourneyInstanceId)).Outcome);
        roles.Active = [RoleCode.Controller];
        Assert.Equal(DriverObservationVerificationOutcome.Malformed, (await adapter.VerifyAsync("controller", Guid.Empty, repository.Assignment.JourneyInstanceId)).Outcome);
        Assert.Equal(DriverObservationVerificationOutcome.MissingOrInaccessible, (await adapter.VerifyAsync("controller", Guid.NewGuid(), repository.Assignment.JourneyInstanceId)).Outcome);
        Assert.Equal(DriverObservationVerificationOutcome.StaleOrIncompatible, (await adapter.VerifyAsync("controller", repository.Observation.ObservationId, Guid.NewGuid())).Outcome);
        history.MissingRevision = true;
        Assert.Equal(DriverObservationVerificationOutcome.StaleOrIncompatible, (await adapter.VerifyAsync("controller", repository.Observation.ObservationId, repository.Assignment.JourneyInstanceId)).Outcome);
        history.MissingRevision = false;
        repository.Assignment.Revoke("operator", At.AddMinutes(1));
        Assert.Equal(DriverObservationVerificationOutcome.StaleOrIncompatible, (await adapter.VerifyAsync("controller", repository.Observation.ObservationId, repository.Assignment.JourneyInstanceId)).Outcome);
        Assert.NotNull(await repository.FindObservationAsync(repository.Observation.ObservationId));
    }

    [Fact]
    public async Task UnavailableRoleOwnerAndJourneyAuthoritiesNeverVerify()
    {
        var repository = new DriverRepository();
        var roles = new Roles { Unavailable = true };
        var history = new Journeys(repository.Assignment.JourneyInstanceId);
        var adapter = new StoredDriverObservationVerifier(repository, new(roles), history);
        Assert.Equal(DriverObservationVerificationOutcome.Unavailable, (await adapter.VerifyAsync("controller", repository.Observation.ObservationId, repository.Assignment.JourneyInstanceId)).Outcome);
        roles.Unavailable = false;
        repository.Unavailable = true;
        Assert.Equal(DriverObservationVerificationOutcome.Unavailable, (await adapter.VerifyAsync("controller", repository.Observation.ObservationId, repository.Assignment.JourneyInstanceId)).Outcome);
        repository.Unavailable = false;
        history.Unavailable = true;
        Assert.Equal(DriverObservationVerificationOutcome.Unavailable, (await adapter.VerifyAsync("controller", repository.Observation.ObservationId, repository.Assignment.JourneyInstanceId)).Outcome);
    }

    private sealed class DriverRepository : IDriverOperationsRepository
    {
        public DriverJourneyAssignment Assignment { get; } = new(Guid.NewGuid(), "driver", Guid.NewGuid(), At, "operator", "manifest");
        public DriverObservation Observation { get; }
        public bool Unavailable { get; set; }
        public DriverRepository() => Observation = new(Guid.NewGuid(), Assignment.AssignmentId, Assignment.JourneyInstanceId, "driver", Guid.NewGuid(), "Progress", At, 1);
        private void Check() { if (Unavailable) throw new InvalidOperationException("owner unavailable"); }
        public Task<DriverJourneyAssignment?> FindActiveAssignmentAsync(string subject, Guid journey, CancellationToken cancellationToken = default) { Check(); return Task.FromResult<DriverJourneyAssignment?>(Assignment.DriverSubject == subject && Assignment.JourneyInstanceId == journey && Assignment.IsActive ? Assignment : null); }
        public Task<DriverJourneyAssignment?> FindAssignmentAsync(Guid id, CancellationToken cancellationToken = default) { Check(); return Task.FromResult<DriverJourneyAssignment?>(id == Assignment.AssignmentId ? Assignment : null); }
        public Task<DriverObservation?> FindObservationAsync(Guid id, CancellationToken cancellationToken = default) { Check(); return Task.FromResult<DriverObservation?>(id == Observation.ObservationId ? Observation : null); }
        public Task<IReadOnlyList<DriverJourneyAssignment>> GetActiveAssignmentsAsync(string? subject, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DriverObservation>> GetObservationsAsync(Guid journey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DriverObservation?> FindRequestAsync(string subject, Guid request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddObservationAsync(DriverObservation observation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task GrantAssignmentAsync(DriverJourneyAssignment assignment, DriverAssignmentAudit audit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> RevokeAssignmentAsync(Guid id, string actor, DateTimeOffset at, Guid auditId, string evidence, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class Roles : IRoleAssignmentRepository
    {
        public RoleCode[] Active { get; set; } = [RoleCode.Controller];
        public bool Unavailable { get; set; }
        public Task<IReadOnlyList<RoleCode>> GetActiveRolesAsync(string subject, CancellationToken cancellationToken = default) => Unavailable ? throw new InvalidOperationException() : Task.FromResult<IReadOnlyList<RoleCode>>(Active);
        public Task<IReadOnlyList<RoleAssignment>> GetActiveAssignmentsAsync(string subject, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task GrantAsync(RoleAssignment assignment, RoleAssignmentAudit audit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> RevokeAsync(Guid id, string actor, DateTimeOffset at, Guid auditId, string? evidence = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class Journeys(Guid journeyId) : IJourneyReadRepository
    {
        public bool MissingRevision { get; set; }
        public bool Unavailable { get; set; }
        public Task<JourneyHistoryReadResult> ReadHistoryAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Unavailable ? new JourneyHistoryReadResult(JourneyReadOutcome.Unavailable) : new JourneyHistoryReadResult(JourneyReadOutcome.Found, MissingRevision ? [] : [new(journeyId, 1, JourneyStatus.Scheduled, At, "ScheduledBaseline")]));
        public Task<JourneySnapshotReadResult> ReadSnapshotAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<JourneyStationDeparturesReadResult> ReadStationDeparturesAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}


