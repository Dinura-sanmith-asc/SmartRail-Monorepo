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

public sealed class DriverRevocationWriteTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
    : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Revocation_between_authorization_precheck_and_write_rejects_stale_observation_atomically()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "revocation-write-race");
        var assignmentId = await DriverOperationsApiTests.GrantAssignmentAsync(host.Services, "racing-driver", journey);
        DriverJourneyAssignment checkedAssignment;
        await using (var precheckScope = host.Services.CreateAsyncScope())
        {
            checkedAssignment = Assert.IsType<DriverJourneyAssignment>(await precheckScope.ServiceProvider
                .GetRequiredService<IDriverOperationsRepository>().FindActiveAssignmentAsync("racing-driver", journey));
            Assert.True(checkedAssignment.IsActive);
        }
        // Represents an application handler that already checked the old active lifecycle.
        var staleObservation = new DriverObservation(Guid.NewGuid(), checkedAssignment.AssignmentId, journey,
            checkedAssignment.DriverSubject, Guid.NewGuid(), "Prechecked before revocation", DateTimeOffset.UtcNow, 1);
        await using (var revokeScope = host.Services.CreateAsyncScope())
            Assert.True(await revokeScope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>()
                .RevokeAssignmentAsync(assignmentId, "trusted-test-maintainer", DateTimeOffset.UtcNow,
                    Guid.NewGuid(), "revocation-before-observation-write"));
        await using (var writeScope = host.Services.CreateAsyncScope())
            await Assert.ThrowsAsync<DriverAssignmentWriteConflictException>(() => writeScope.ServiceProvider
                .GetRequiredService<IDriverOperationsRepository>().AddObservationAsync(staleObservation));
        await using var verification = host.Services.CreateAsyncScope();
        var repository = verification.ServiceProvider.GetRequiredService<IDriverOperationsRepository>();
        Assert.Null(await repository.FindActiveAssignmentAsync("racing-driver", journey));
        Assert.Empty(await repository.GetObservationsAsync(journey));
        var db = verification.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Single(await db.Set<DriverJourneyAssignment>().ToListAsync());
        var audits = await db.Set<DriverAssignmentAudit>().Where(x => x.AssignmentId == assignmentId).ToListAsync();
        Assert.Equal(2, audits.Count);
        Assert.Single(audits, x => x.Action == DriverAssignmentAuditAction.Granted);
        Assert.Single(audits, x => x.Action == DriverAssignmentAuditAction.Revoked &&
            x.EvidenceReference == "revocation-before-observation-write");
    }
}
