using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.DriverOperations;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Domain.DriverOperations;
using SmartRail.Domain.DriverOperations.Entities;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.Incidents;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.DriverOperations;

public sealed class DriverOperationsApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
    : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    private const string Driver = "f005-verified-driver";
    private const string Controller = "f005-verified-controller";

    [Fact]
    public async Task Signed_driver_reads_only_own_assignments_and_submits_replayable_non_authoritative_evidence()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "driver-primary");
        var other = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "driver-other");
        await IdentityTestData.GrantRoleAsync(host.Services, Driver, RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, "another-driver", RoleCode.Driver);
        await GrantAssignmentAsync(host.Services, Driver, journey);
        await GrantAssignmentAsync(host.Services, "another-driver", other);
        using var assignmentsRequest = IdentityTestData.Bearer(HttpMethod.Get, "/api/driver/assignments", oidc.CreateToken(Driver));
        using var assignments = await client.SendAsync(assignmentsRequest);
        Assert.Equal(HttpStatusCode.OK, assignments.StatusCode);
        using var list = JsonDocument.Parse(await assignments.Content.ReadAsStringAsync());
        var item = Assert.Single(list.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(Driver, item.GetProperty("driverSubject").GetString());
        Assert.Equal(journey, item.GetProperty("journey").GetProperty("snapshot").GetProperty("journeyInstanceId").GetGuid());

        var requestId = Guid.NewGuid();
        using var created = await SubmitAsync(client, journey, Driver, requestId, "  Observation for Controller review  ");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var evidence = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Assert.Equal("Observation for Controller review", evidence.RootElement.GetProperty("text").GetString());
        Assert.Equal(Driver, evidence.RootElement.GetProperty("driverSubject").GetString());
        using var replay = await SubmitAsync(client, journey, Driver, requestId, "Observation for Controller review");
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var replayBody = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        Assert.Equal(evidence.RootElement.GetProperty("observationId").GetGuid(), replayBody.RootElement.GetProperty("observationId").GetGuid());
        using var conflict = await SubmitAsync(client, journey, Driver, requestId, "Different payload");
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var denied = await SubmitAsync(client, other, Driver, Guid.NewGuid(), "Not assigned");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>().GetObservationsAsync(journey));
        Assert.Equal(1, await db.Journeys.Where(x => x.JourneyInstanceId == journey).Select(x => x.CurrentRevision).SingleAsync());
        Assert.Equal(1, await db.JourneyRevisions.CountAsync(x => x.JourneyInstanceId == journey));
        using var publicJourney = await client.GetAsync($"/api/journeys/{journey}");
        var publicBody = await publicJourney.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Driver, publicBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Observation for Controller", publicBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Controller_reviews_protected_evidence_but_driver_and_other_roles_cannot_review_or_publish()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "review");
        await IdentityTestData.GrantRoleAsync(host.Services, Driver, RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, Controller, RoleCode.Controller);
        await GrantAssignmentAsync(host.Services, Driver, journey);
        using var created = await SubmitAsync(client, journey, Driver, Guid.NewGuid(), "Driver evidence");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var evidence = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var observationId = evidence.RootElement.GetProperty("observationId").GetGuid();
        using var review = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get,
            $"/api/operations/journeys/{journey}/observations", oidc.CreateToken(Controller)));
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        using var reviewBody = JsonDocument.Parse(await review.Content.ReadAsStringAsync());
        Assert.Equal(observationId, Assert.Single(reviewBody.RootElement.GetProperty("items").EnumerateArray()).GetProperty("observationId").GetGuid());
        foreach (var role in new[] { RoleCode.Driver, RoleCode.Passenger, RoleCode.StationStaff })
        {
            var subject = "review-denied-" + role;
            await IdentityTestData.GrantRoleAsync(host.Services, subject, role);
            using var denied = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get,
                $"/api/operations/journeys/{journey}/observations", oidc.CreateToken(subject)));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        using var controllerSubmit = await SubmitAsync(client, journey, Controller, Guid.NewGuid(), "Not a driver");
        Assert.Equal(HttpStatusCode.Forbidden, controllerSubmit.StatusCode);
        using var publication = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journey}/status", oidc.CreateToken(Controller));
        publication.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Delayed", driverObservationId = observationId });
        using var published = await client.SendAsync(publication);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        await using var scope = host.Services.CreateAsyncScope();
        var verifier = scope.ServiceProvider.GetRequiredService<IDriverObservationVerifier>();
        Assert.Equal(DriverObservationVerificationOutcome.Verified,
            (await verifier.VerifyAsync(Controller, observationId, journey)).Outcome);
    }

    [Fact]
    public async Task Assignment_and_role_revocation_are_rechecked_even_on_identical_replay()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "revoke");
        var roleId = await IdentityTestData.GrantRoleAsync(host.Services, Driver, RoleCode.Driver);
        var assignmentId = await GrantAssignmentAsync(host.Services, Driver, journey);
        var requestId = Guid.NewGuid();
        using var first = await SubmitAsync(client, journey, Driver, requestId, "Before revocation");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        await using (var scope = host.Services.CreateAsyncScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>()
                .RevokeAssignmentAsync(assignmentId, "test-maintainer", DateTimeOffset.UtcNow, Guid.NewGuid(), "revocation-evidence"));
        using var deniedReplay = await SubmitAsync(client, journey, Driver, requestId, "Before revocation");
        Assert.Equal(HttpStatusCode.Forbidden, deniedReplay.StatusCode);
        await GrantAssignmentAsync(host.Services, Driver, journey);
        await using (var scope = host.Services.CreateAsyncScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<IRoleAssignmentRepository>()
                .RevokeAsync(roleId, "test-maintainer", DateTimeOffset.UtcNow, Guid.NewGuid(), "role-revoked"));
        using var deniedRole = await SubmitAsync(client, journey, Driver, Guid.NewGuid(), "After revocation");
        Assert.Equal(HttpStatusCode.Forbidden, deniedRole.StatusCode);
        await using var finalScope = host.Services.CreateAsyncScope();
        Assert.Single(await finalScope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>().GetObservationsAsync(journey));
    }

    [Fact]
    public async Task Malformed_input_missing_or_expired_credentials_and_unavailable_authority_never_write()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "negative");
        await IdentityTestData.GrantRoleAsync(host.Services, Driver, RoleCode.Driver);
        await GrantAssignmentAsync(host.Services, Driver, journey);
        foreach (var text in new[] { "", "   ", new string('x', 4001) })
        {
            using var invalid = await SubmitAsync(client, journey, Driver, Guid.NewGuid(), text);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using var emptyId = await SubmitAsync(client, journey, Driver, Guid.Empty, "Invalid request identity");
        Assert.Equal(HttpStatusCode.BadRequest, emptyId.StatusCode);
        using var missing = await client.GetAsync("/api/driver/assignments");
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        using var expired = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/driver/assignments",
            oidc.CreateToken(Driver, notBefore: DateTime.UtcNow.AddHours(-2), expires: DateTime.UtcNow.AddHours(-1))));
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        await using var scope = host.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>().GetObservationsAsync(journey));
        using var unavailableHost = await IncidentApiTestHost.CreateAsync(database, oidc,
            services => services.AddScoped<IRoleAssignmentRepository, UnavailableRoleAssignmentRepository>());
        using var unavailableClient = unavailableHost.CreateClient();
        using var unavailable = await SubmitAsync(unavailableClient, journey, Driver, Guid.NewGuid(), "No authority");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
    }

    [Fact]
    public async Task Concurrent_identical_requests_create_one_observation_and_conflicting_reuse_changes_nothing()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "concurrent");
        await IdentityTestData.GrantRoleAsync(host.Services, Driver, RoleCode.Driver);
        await GrantAssignmentAsync(host.Services, Driver, journey);
        var requestId = Guid.NewGuid();
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => SubmitAsync(client, journey, Driver, requestId, "Same concurrent evidence")));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            Assert.All(responses.Where(x => x.StatusCode != HttpStatusCode.Created), x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
            var ids = new List<Guid>();
            foreach (var response in responses)
            {
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                ids.Add(body.RootElement.GetProperty("observationId").GetGuid());
            }
            Assert.Single(ids.Distinct());
        }
        finally { foreach (var response in responses) response.Dispose(); }
        await using var scope = host.Services.CreateAsyncScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>().GetObservationsAsync(journey));
    }

    [Fact]
    public async Task Caller_supplied_authority_fields_are_rejected_and_foreign_or_revoked_observation_never_authorizes_publication()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "verified-evidence");
        var foreignJourney = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "foreign-evidence");
        await IdentityTestData.GrantRoleAsync(host.Services, Driver, RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, Controller, RoleCode.Controller);
        var assignment = await GrantAssignmentAsync(host.Services, Driver, journey);
        using var forged = IdentityTestData.Bearer(HttpMethod.Post, $"/api/driver/journeys/{journey}/observations", oidc.CreateToken(Driver));
        forged.Content = JsonContent.Create(new { requestId = Guid.NewGuid(), text = "Forged metadata", driverSubject = Controller, status = "Cancelled" });
        using var forgedResponse = await client.SendAsync(forged);
        Assert.Equal(HttpStatusCode.BadRequest, forgedResponse.StatusCode);
        using var created = await SubmitAsync(client, journey, Driver, Guid.NewGuid(), "Genuine evidence");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var observation = createdBody.RootElement.GetProperty("observationId").GetGuid();
        foreach (var (target, reference, expected) in new[]
        {
            (foreignJourney, observation, HttpStatusCode.Conflict),
            (journey, Guid.NewGuid(), HttpStatusCode.NotFound)
        })
        {
            using var request = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{target}/status", oidc.CreateToken(Controller));
            request.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Delayed", driverObservationId = reference });
            using var denied = await client.SendAsync(request);
            Assert.Equal(expected, denied.StatusCode);
        }
        await using (var scope = host.Services.CreateAsyncScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>()
                .RevokeAssignmentAsync(assignment, "test-maintainer", DateTimeOffset.UtcNow, Guid.NewGuid(), "revoked-evidence"));
        using var revokedRequest = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journey}/status", oidc.CreateToken(Controller));
        revokedRequest.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Delayed", driverObservationId = observation });
        using var revoked = await client.SendAsync(revokedRequest);
        Assert.Equal(HttpStatusCode.Conflict, revoked.StatusCode);
        await using var finalScope = host.Services.CreateAsyncScope();
        var db = finalScope.ServiceProvider.GetRequiredService<SmartRailDbContext>();
        Assert.All(await db.Journeys.ToListAsync(), x => Assert.Equal(1, x.CurrentRevision));
    }

    [Fact]
    public async Task Current_central_publication_refreshes_driver_duty_and_controller_reads_all_current_assignments()
    {
        using var host = await IncidentOwnerIntegrationTestHost.CreateAsync(database, oidc);
        using var client = host.CreateClient();
        var journey = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "duty-refresh");
        var otherJourney = await IncidentOwnerIntegrationTestHost.InitializeJourneyAsync(host.Services, "second-duty");
        await IdentityTestData.GrantRoleAsync(host.Services, Driver, RoleCode.Driver);
        await IdentityTestData.GrantRoleAsync(host.Services, Controller, RoleCode.Controller);
        await GrantAssignmentAsync(host.Services, Driver, journey);
        await GrantAssignmentAsync(host.Services, "second-driver", otherJourney);
        using var controllerDuties = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/driver/assignments", oidc.CreateToken(Controller)));
        Assert.Equal(HttpStatusCode.OK, controllerDuties.StatusCode);
        using var all = JsonDocument.Parse(await controllerDuties.Content.ReadAsStringAsync());
        Assert.Equal(2, all.RootElement.GetProperty("items").GetArrayLength());
        using var publication = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/operations/journeys/{journey}/status", oidc.CreateToken(Controller));
        publication.Content = JsonContent.Create(new { expectedJourneyRevision = 1, status = "Delayed" });
        using var changed = await client.SendAsync(publication);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        using var duty = await client.SendAsync(IdentityTestData.Bearer(HttpMethod.Get, "/api/driver/assignments", oidc.CreateToken(Driver)));
        Assert.Equal(HttpStatusCode.OK, duty.StatusCode);
        using var body = JsonDocument.Parse(await duty.Content.ReadAsStringAsync());
        var context = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray()).GetProperty("journey");
        Assert.Equal(2, context.GetProperty("snapshot").GetProperty("revision").GetInt32());
        Assert.Equal("Delayed", context.GetProperty("snapshot").GetProperty("status").GetString());
        Assert.Equal(2, Assert.Single(context.GetProperty("updates").EnumerateArray()).GetProperty("revision").GetInt32());
    }

    private async Task<HttpResponseMessage> SubmitAsync(HttpClient client, Guid journeyId, string subject, Guid requestId, string text)
    {
        using var request = IdentityTestData.Bearer(HttpMethod.Post, $"/api/driver/journeys/{journeyId}/observations", oidc.CreateToken(subject));
        request.Content = JsonContent.Create(new { requestId, text });
        return await client.SendAsync(request);
    }

    internal static async Task<Guid> GrantAssignmentAsync(IServiceProvider services, string subject, Guid journey)
    {
        await using var scope = services.CreateAsyncScope();
        var assignment = new DriverJourneyAssignment(Guid.NewGuid(), subject, journey, DateTimeOffset.UtcNow.AddMinutes(-1),
            "test-assignment-maintainer", "isolated-test-manifest");
        var audit = new DriverAssignmentAudit(Guid.NewGuid(), assignment.AssignmentId, DriverAssignmentAuditAction.Granted,
            assignment.AssignedBySubject, assignment.AssignedAtUtc, assignment.SourceReference);
        await scope.ServiceProvider.GetRequiredService<IDriverOperationsRepository>().GrantAssignmentAsync(assignment, audit);
        return assignment.AssignmentId;
    }
}
