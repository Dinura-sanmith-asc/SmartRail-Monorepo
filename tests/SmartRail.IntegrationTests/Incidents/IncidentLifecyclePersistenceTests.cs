using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentLifecyclePersistenceTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Forward_transitions_persist_actor_time_revision_and_immutable_evidence()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var subject = "f004-lifecycle-persistence-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "lifecycle-create-" + Guid.NewGuid());
        var token = oidc.CreateToken(subject);

        foreach (var (revision, state) in new[]
                 {
                     (1, "In Progress"), (2, "Resolved"), (3, "Closed")
                 })
        {
            using var response = await client.SendAsync(IncidentReadTestSupport.Patch(incidentId, revision, state,
                token, $"lifecycle-{revision}-{Guid.NewGuid():N}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(state, body.RootElement.GetProperty("state").GetString());
            Assert.Equal(revision + 1, body.RootElement.GetProperty("revision").GetInt32());
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            var incident = await db.Incidents.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
            Assert.Equal(IncidentState.Closed, incident.State);
            Assert.Equal(4, incident.Revision);
            var revisions = await db.IncidentRevisions.AsNoTracking().Where(x => x.IncidentId == incidentId)
                .OrderBy(x => x.Revision).ToArrayAsync();
            Assert.Equal(new[] { 1, 2, 3, 4 }, revisions.Select(x => x.Revision));
            Assert.Equal(new IncidentState?[] { null, IncidentState.Reported, IncidentState.InProgress, IncidentState.Resolved },
                revisions.Select(x => x.PreviousState));
            Assert.Equal(new[] { IncidentState.Reported, IncidentState.InProgress, IncidentState.Resolved, IncidentState.Closed },
                revisions.Select(x => x.State));
            Assert.All(revisions, revision =>
            {
                Assert.Equal(subject, revision.ActorSubject);
                Assert.Equal("IncidentManagement", revision.Source);
                Assert.Equal(TimeSpan.Zero, revision.OccurredAt.Offset);
                Assert.False(string.IsNullOrWhiteSpace(revision.AssociationEvidence));
            });
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            var revision = await db.IncidentRevisions.SingleAsync(x => x.IncidentId == incidentId && x.Revision == 2);
            db.Entry(revision).Property(x => x.ActorSubject).CurrentValue = "tampered";
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            var revision = await db.IncidentRevisions.SingleAsync(x => x.IncidentId == incidentId && x.Revision == 2);
            db.IncidentRevisions.Remove(revision);
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            Assert.Equal(4, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
        }
    }

    [Fact]
    public async Task Failed_write_rolls_back_state_revision_and_idempotency_record()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var subject = "f004-lifecycle-rollback-" + Guid.NewGuid().ToString("N");
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var incidentId = await IncidentReadTestSupport.CreateIncidentAsync(client, oidc, subject, stationId,
            "lifecycle-rollback-create-" + Guid.NewGuid());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            var incident = await db.Incidents.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
            incident.TransitionTo(IncidentState.InProgress, 1, DateTimeOffset.UtcNow);
            var evidence = await db.IncidentAssociations.AsNoTracking().Where(x => x.IncidentId == incidentId)
                .Select(x => new { x.AssociationId, Kind = x.Kind.ToString(), x.ResourceId }).ToArrayAsync();
            var revision = new IncidentRevision(incidentId, 2, IncidentState.InProgress, subject,
                DateTimeOffset.UtcNow, JsonSerializer.Serialize(evidence), IncidentState.Reported);
            var idempotency = new IncidentIdempotencyRecord(Guid.NewGuid(), subject,
                $"PATCH /api/incidents/{incidentId:D}", "force-rollback-" + Guid.NewGuid(), "rollback-hash",
                incidentId, 200, "{}", DateTimeOffset.UtcNow);
            var repository = scope.ServiceProvider.GetRequiredService<SmartRail.Application.Incidents.Persistence.IIncidentRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<SmartRail.Application.Incidents.Persistence.IIncidentUnitOfWork>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteAsync<bool>(async ct =>
            {
                await repository.UpdateAsync(incident, revision, Array.Empty<IncidentAssociation>(),
                    Array.Empty<IncidentAssociation>(), idempotency, 1, ct);
                throw new InvalidOperationException("test rollback after write");
            }));
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
            var incident = await db.Incidents.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
            Assert.Equal(IncidentState.Reported, incident.State);
            Assert.Equal(1, incident.Revision);
            Assert.Equal(1, await db.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
            Assert.Equal(1, await db.IncidentIdempotencyRecords.CountAsync(x => x.IncidentId == incidentId));
        }
    }
}
