using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Incidents.References;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Application.Incidents.Persistence;
using SmartRail.Infrastructure.Incidents.References;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.JourneysStatus.Fixtures;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentAssociationPersistenceTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Genuine_owner_records_verify_and_persist_two_journeys_and_published_disruption()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        const string controller = "f004-owner-positive-" + nameof(Genuine_owner_records_verify_and_persist_two_journeys_and_published_disruption);
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, controller, RoleCode.Controller);
        var firstJourney = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "journey-a-" + Guid.NewGuid().ToString("N"));
        var secondJourney = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(factory.Services, "journey-b-" + Guid.NewGuid().ToString("N"));
        Assert.NotEqual(firstJourney, secondJourney);
        var token = oidc.CreateToken(controller);
        var updateId = await IncidentOwnerTestSupport.PublishJourneyChangeAsync(client, token, firstJourney, 1, "Delayed");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            Assert.IsType<Feature001IncidentReferenceVerifier>(scope.ServiceProvider.GetRequiredService<IFeature001IncidentReferenceVerifier>());
            Assert.IsType<Feature003IncidentReferenceVerifier>(scope.ServiceProvider.GetRequiredService<IFeature003IncidentReferenceVerifier>());
            var stationCheck = await scope.ServiceProvider.GetRequiredService<IFeature001IncidentReferenceVerifier>()
                .VerifyAsync(new(IncidentReferenceKind.Station, IncidentOwnerTestSupport.StationA));
            var serviceCheck = await scope.ServiceProvider.GetRequiredService<IFeature001IncidentReferenceVerifier>()
                .VerifyAsync(new(IncidentReferenceKind.Service, IncidentOwnerTestSupport.Service));
            var journeyCheck = await scope.ServiceProvider.GetRequiredService<IFeature003IncidentReferenceVerifier>()
                .VerifyAsync(new(IncidentReferenceKind.Journey, secondJourney));
            var disruptionCheck = await scope.ServiceProvider.GetRequiredService<IFeature003IncidentReferenceVerifier>()
                .VerifyAsync(new(IncidentReferenceKind.Disruption, firstJourney, updateId, 2));
            Assert.All(new[] { stationCheck, serviceCheck, journeyCheck, disruptionCheck }, check =>
                Assert.Equal(IncidentReferenceOutcome.Verified, check.Outcome));
        }

        var incidentId = await IncidentOwnerTestSupport.CreateIncidentAsync(client, token, Guid.NewGuid().ToString("N"),
        [
            IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA),
            IncidentOwnerTestSupport.Link("Service", IncidentOwnerTestSupport.Service),
            IncidentOwnerTestSupport.Link("Journey", firstJourney),
            IncidentOwnerTestSupport.Link("Journey", secondJourney),
            IncidentOwnerTestSupport.Link("Disruption", firstJourney, updateId, 2)
        ]);

        using var get = new HttpRequestMessage(HttpMethod.Get, $"/api/incidents?incidentId={incidentId:D}");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var incident = Assert.Single(result.RootElement.GetProperty("items").EnumerateArray());
        var links = incident.GetProperty("associations").EnumerateArray().ToArray();
        Assert.Equal(5, links.Length);
        Assert.Equal(new[] { firstJourney, secondJourney }.Order().ToArray(), links.Where(link => link.GetProperty("kind").GetString() == "Journey")
            .Select(link => link.GetProperty("resourceId").GetGuid()).Order().ToArray());
        var disruption = Assert.Single(links, link => link.GetProperty("kind").GetString() == "Disruption");
        Assert.Equal(updateId, disruption.GetProperty("updateId").GetGuid());
        Assert.Equal(2, disruption.GetProperty("revision").GetInt32());

        await using var dbScope = factory.Services.CreateAsyncScope();
        var db = dbScope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(2, await db.Journeys.Where(x => x.ServiceId == JourneyFixtureBuilder.ServiceId).CountAsync());
        Assert.Equal(1, await db.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == firstJourney && x.Revision == 2));

        var originalStationLink = await db.IncidentAssociations.SingleAsync(x => x.IncidentId == incidentId &&
            x.Kind == IncidentAssociationKind.Station && x.ResourceId == IncidentOwnerTestSupport.StationA);
        var replacementStation = await IncidentOwnerTestSupport.AddPublishedStationAsync(factory.Services);
        using (var delta = await client.SendAsync(IncidentOwnerTestSupport.PatchIncident(incidentId, token,
                   "atomic-owner-delta", 1, add: [IncidentOwnerTestSupport.Link("Station", replacementStation)],
                   remove: [originalStationLink.AssociationId])))
            Assert.Equal(System.Net.HttpStatusCode.OK, delta.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verificationDb = verifyScope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        var historicalLink = await verificationDb.IncidentAssociations.AsNoTracking().SingleAsync(x => x.AssociationId == originalStationLink.AssociationId);
        var replacementLink = await verificationDb.IncidentAssociations.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId && x.ResourceId == replacementStation);
        Assert.Equal(2, historicalLink.RemovedRevision);
        Assert.Equal(2, replacementLink.AddedRevision);
        Assert.NotEqual(historicalLink.AssociationId, replacementLink.AssociationId);
        using (var duplicate = await client.SendAsync(IncidentOwnerTestSupport.PatchIncident(incidentId, token,
                   "duplicate-active-owner-link", 2, add: [IncidentOwnerTestSupport.Link("Service", IncidentOwnerTestSupport.Service)])))
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal(2, await verificationDb.Incidents.Where(x => x.IncidentId == incidentId).Select(x => x.Revision).SingleAsync());
    }

    [Fact]
    public async Task Association_delta_transaction_rolls_back_aggregate_revision_and_all_evidence_after_foreign_key_failure()
    {
        await using var factory = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = factory.CreateClient();
        var subject = "f004-association-rollback-" + Guid.NewGuid().ToString("N");
        await IncidentOwnerTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var token = oidc.CreateToken(subject);
        var incidentId = await IncidentOwnerTestSupport.CreateIncidentAsync(client, token, Guid.NewGuid().ToString("N"),
            [IncidentOwnerTestSupport.Link("Station", IncidentOwnerTestSupport.StationA)]);
        var requestKey = "forced-association-rollback-" + Guid.NewGuid().ToString("N");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<SmartRailDbContext>();
            var incident = await db.Incidents.AsNoTracking().SingleAsync(x => x.IncidentId == incidentId);
            var now = DateTimeOffset.UtcNow;
            incident.AdvanceRevisionForAssociationChange(1, now);
            var newAssociation = new IncidentAssociation(Guid.NewGuid(), incidentId, IncidentAssociationKind.Station,
                Guid.NewGuid(), now, 2); // Invalid Feature 001 foreign key forces a late transaction failure.
            var revision = new IncidentRevision(incidentId, 2, incident.State, subject, now, "[]");
            var idempotency = new IncidentIdempotencyRecord(Guid.NewGuid(), subject,
                $"PATCH /api/incidents/{incidentId:D}", requestKey, "test-payload-hash", incidentId, 200, "{}", now);
            var repository = services.GetRequiredService<IIncidentRepository>();
            var unitOfWork = services.GetRequiredService<IIncidentUnitOfWork>();
            await Assert.ThrowsAnyAsync<Exception>(() => unitOfWork.ExecuteAsync(ct =>
                repository.UpdateAsync(incident, revision, [newAssociation], [], idempotency, 1, ct)));
        }

        await using var verify = factory.Services.CreateAsyncScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Equal(1, await verifyDb.Incidents.Where(x => x.IncidentId == incidentId).Select(x => x.Revision).SingleAsync());
        Assert.Equal(1, await verifyDb.IncidentRevisions.CountAsync(x => x.IncidentId == incidentId));
        Assert.Equal(1, await verifyDb.IncidentAssociations.CountAsync(x => x.IncidentId == incidentId));
        Assert.False(await verifyDb.IncidentIdempotencyRecords.AnyAsync(x => x.IdempotencyKey == requestKey));
    }
}
