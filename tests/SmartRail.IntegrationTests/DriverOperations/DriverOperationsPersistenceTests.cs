using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.DriverOperations;
using SmartRail.Domain.DriverOperations;
using SmartRail.Domain.DriverOperations.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.Incidents;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.DriverOperations;

public sealed class DriverOperationsPersistenceTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
    : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Migration_enforces_active_assignment_uniqueness_and_revocation_retains_audited_history()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "constraints");
        var first = await DriverOperationsApiTests.GrantAssignmentAsync(host.Services, "persisted-driver", journey);
        await Assert.ThrowsAnyAsync<Exception>(() => DriverOperationsApiTests.GrantAssignmentAsync(host.Services, "persisted-driver", journey));
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>();
            Assert.True(await repository.RevokeAssignmentAsync(first, "test-maintainer", DateTimeOffset.UtcNow,
                Guid.NewGuid(), "approved-revocation"));
            Assert.Empty(await repository.GetActiveAssignmentsAsync("persisted-driver"));
        }
        var second = await DriverOperationsApiTests.GrantAssignmentAsync(host.Services, "persisted-driver", journey);
        Assert.NotEqual(first, second);
        await using var verification = host.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, await db.Set<DriverJourneyAssignment>().CountAsync());
        Assert.Equal(3, await db.Set<DriverAssignmentAudit>().CountAsync());
        Assert.Equal(second, Assert.Single(await verification.ServiceProvider.GetRequiredService<IDriverOperationsRepository>()
            .GetActiveAssignmentsAsync("persisted-driver")).AssignmentId);
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), x => x.EndsWith("_DriverOperations", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Failed_audit_foreign_key_rolls_back_assignment_and_does_not_leave_partial_state()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "rollback");
        var assignment = new DriverJourneyAssignment(Guid.NewGuid(), "rollback-driver", journey, DateTimeOffset.UtcNow,
            "test-maintainer", "test-source");
        var invalidAudit = new DriverAssignmentAudit(Guid.NewGuid(), Guid.NewGuid(), DriverAssignmentAuditAction.Granted,
            assignment.AssignedBySubject, assignment.AssignedAtUtc, "mismatched-assignment");
        await using (var scope = host.Services.CreateAsyncScope())
            await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>()
                .GrantAssignmentAsync(assignment, invalidAudit));
        await using var verification = host.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(0, await db.Set<DriverJourneyAssignment>().CountAsync());
        Assert.Equal(0, await db.Set<DriverAssignmentAudit>().CountAsync());
    }

    [Fact]
    public async Task Observations_have_restrictive_foreign_keys_unique_retry_identity_and_immutable_history()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "immutable");
        var assignment = await DriverOperationsApiTests.GrantAssignmentAsync(host.Services, "immutable-driver", journey);
        var request = Guid.NewGuid();
        var observation = new DriverObservation(Guid.NewGuid(), assignment, journey, "immutable-driver", request,
            "Original authored evidence", DateTimeOffset.UtcNow, 1);
        await using (var scope = host.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>().AddObservationAsync(observation);
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            var tracked = await db.Set<DriverObservation>().SingleAsync();
            db.Entry(tracked).Property(x => x.Text).CurrentValue = "Unauthorized rewrite";
            await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
        }
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            db.Remove(await db.Set<DriverObservation>().SingleAsync());
            await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
        }
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var duplicate = new DriverObservation(Guid.NewGuid(), assignment, journey, "immutable-driver", request,
                "Duplicate retry identity", DateTimeOffset.UtcNow, 1);
            await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>()
                .AddObservationAsync(duplicate));
        }
        await using var verification = host.Services.CreateAsyncScope();
        var record = Assert.Single(await verification.ServiceProvider.GetRequiredService<IDriverOperationsRepository>().GetObservationsAsync(journey));
        Assert.Equal("Original authored evidence", record.Text);
    }
}
