using SmartRail.Application.DriverOperations;
using SmartRail.Domain.DriverOperations.Entities;
using Xunit;

namespace SmartRail.UnitTests.DriverOperations;

public sealed class DriverApplicationBoundaryTests
{
    [Fact]
    public async Task Default_maintenance_authority_denies_grant_and_revoke_before_any_owner_or_repository_call()
    {
        var repository = new RejectRepository();
        var handler = new DriverAssignmentMaintenanceHandler(repository,
            new DenyAllDriverAssignmentMaintenanceAuthority(), null!, TimeProvider.System);
        var grant = await handler.GrantAsync(new("operator", Guid.NewGuid(), "driver", Guid.NewGuid(), "source"));
        var revoke = await handler.RevokeAsync(new("operator", Guid.NewGuid(), "source"));
        Assert.Equal(DriverOutcome.Forbidden, grant.Outcome);
        Assert.Equal(DriverOutcome.Forbidden, revoke.Outcome);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Unavailable_maintenance_authority_fails_closed_without_repository_calls()
    {
        var repository = new RejectRepository();
        var handler = new DriverAssignmentMaintenanceHandler(repository, new UnavailableAuthority(), null!, TimeProvider.System);
        Assert.Equal(DriverOutcome.Unavailable, (await handler.GrantAsync(new("operator", Guid.NewGuid(), "driver", Guid.NewGuid(), "source"))).Outcome);
        Assert.Equal(DriverOutcome.Unavailable, (await handler.RevokeAsync(new("operator", Guid.NewGuid(), "source"))).Outcome);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Anonymous_calls_never_touch_roles_owner_or_repository()
    {
        var handler = new DriverOperationsHandler(null!, null!, null!, null!, null!, TimeProvider.System);
        Assert.Equal(DriverOutcome.Unauthorized, (await handler.GetAssignmentsAsync(null)).Outcome);
        Assert.Equal(DriverOutcome.Unauthorized, (await handler.GetObservationsAsync(null, Guid.NewGuid())).Outcome);
        Assert.Equal(DriverOutcome.Unauthorized, (await handler.SubmitObservationAsync(null, Guid.NewGuid(), Guid.NewGuid(), "report")).Outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_observation_text_is_invalid_before_any_protected_write(string? text)
    {
        var handler = new DriverOperationsHandler(null!, null!, null!, null!, null!, TimeProvider.System);
        Assert.Equal(DriverOutcome.Invalid, (await handler.SubmitObservationAsync("driver", Guid.NewGuid(), Guid.NewGuid(), text)).Outcome);
    }

    [Fact]
    public async Task Empty_identifiers_and_overlong_text_are_rejected_without_repository_calls()
    {
        var handler = new DriverOperationsHandler(null!, null!, null!, null!, null!, TimeProvider.System);
        Assert.Equal(DriverOutcome.Invalid, (await handler.SubmitObservationAsync("driver", Guid.Empty, Guid.NewGuid(), "report")).Outcome);
        Assert.Equal(DriverOutcome.Invalid, (await handler.SubmitObservationAsync("driver", Guid.NewGuid(), Guid.Empty, "report")).Outcome);
        Assert.Equal(DriverOutcome.Invalid, (await handler.SubmitObservationAsync("driver", Guid.NewGuid(), Guid.NewGuid(), new string('x', 4001))).Outcome);
        Assert.Equal(DriverOutcome.Invalid, (await handler.GetObservationsAsync("controller", Guid.Empty)).Outcome);
    }

    private sealed class UnavailableAuthority : IDriverAssignmentMaintenanceAuthority
    {
        public Task<DriverAssignmentMaintenanceOutcome> AuthorizeAsync(string actor, DriverAssignmentMaintenanceAction action,
            CancellationToken cancellationToken = default) => Task.FromResult(DriverAssignmentMaintenanceOutcome.Unavailable);
    }
    private sealed class RejectRepository : IDriverOperationsRepository
    {
        public int Calls { get; private set; }
        private Exception Unexpected() { Calls++; return new InvalidOperationException("A denied command must not call persistence."); }
        public Task<IReadOnlyList<DriverJourneyAssignment>> GetActiveAssignmentsAsync(string? subject, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<DriverJourneyAssignment?> FindActiveAssignmentAsync(string subject, Guid id, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<DriverJourneyAssignment?> FindAssignmentAsync(Guid id, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<IReadOnlyList<DriverObservation>> GetObservationsAsync(Guid id, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<DriverObservation?> FindObservationAsync(Guid id, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<DriverObservation?> FindRequestAsync(string subject, Guid id, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task AddObservationAsync(DriverObservation observation, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task GrantAssignmentAsync(DriverJourneyAssignment assignment, DriverAssignmentAudit audit, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<bool> RevokeAssignmentAsync(Guid id, string actor, DateTimeOffset at, Guid auditId, string evidence, CancellationToken cancellationToken = default) => throw Unexpected();
    }
}
