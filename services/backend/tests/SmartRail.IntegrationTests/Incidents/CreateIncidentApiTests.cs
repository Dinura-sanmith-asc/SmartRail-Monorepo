using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Api.Contracts.Incidents;
using SmartRail.Application.Incidents.References;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class CreateIncidentApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Create_maps_malformed_unknown_category_missing_token_and_invalid_token_safely()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        var subject = "f004-create-invalid-api";
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var token = oidc.CreateToken(subject);

        using (var malformed = new HttpRequestMessage(HttpMethod.Post, "/api/incidents")
        {
            Content = new StringContent("{invalid", Encoding.UTF8, "application/json")
        })
        {
            malformed.Headers.Add("Idempotency-Key", "bad-json");
            malformed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(malformed);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        using (var unknownCategory = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), category: "NOT_APPROVED", token: token)))
            Assert.Equal(HttpStatusCode.BadRequest, unknownCategory.StatusCode);
        using (var noToken = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString())))
            Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
        using (var invalidToken = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: "not-a-jwt")))
            Assert.Equal(HttpStatusCode.Unauthorized, invalidToken.StatusCode);
    }

    [Fact]
    public async Task Caller_cannot_supply_server_identity_or_report_fields()
    {
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureValidTestAuthorities);
        using var client = factory.CreateClient();
        const string subject = "f004-create-untrusted-fields";
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/incidents")
        {
            Content = new StringContent($"{{\"categoryCode\":\"{IncidentTestCategories.TrackObstruction}\",\"description\":\"valid description\",\"reporterSubject\":\"spoofed\",\"associations\":[{{\"kind\":\"Station\",\"resourceId\":\"{stationId}\"}}]}}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", "spoofed-reporter");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oidc.CreateToken(subject));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Owner_authority_and_unconfigured_production_category_fail_closed()
    {
        await using (var ownerUnavailableFactory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            services.Configure<SmartRail.Infrastructure.Incidents.Categories.IncidentCategoryCatalogOptions>(options => options.Codes = IncidentTestCategories.All.ToArray());
            services.RemoveAll<IFeature001IncidentReferenceVerifier>();
            services.AddScoped<IFeature001IncidentReferenceVerifier, UnavailableTestVerifier>();
        }))
        using (var client = ownerUnavailableFactory.CreateClient())
        {
            const string subject = "f004-create-owner-down";
            await IncidentCreationTestSupport.GrantRoleAsync(ownerUnavailableFactory.Services, subject, RoleCode.Controller);
            var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(ownerUnavailableFactory.Services);
            using var response = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: oidc.CreateToken(subject)));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        await using (var noCategoryFactory = await IncidentApiTestHost.CreateAsync(database, oidc,
            IncidentCreationTestSupport.ConfigureTestOwnerVerifier))
        using (var client = noCategoryFactory.CreateClient())
        {
            const string subject = "f004-create-no-category-catalog";
            await IncidentCreationTestSupport.GrantRoleAsync(noCategoryFactory.Services, subject, RoleCode.Controller);
            var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(noCategoryFactory.Services);
            using var response = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(), token: oidc.CreateToken(subject)));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
    }

    [Fact]
    public void API_state_label_preserves_approved_in_progress_spacing()
    {
        Assert.Equal("In Progress", IncidentResponse.ToStateLabel("InProgress"));
        Assert.Equal("In Progress", IncidentResponse.ToStateLabel("In Progress"));
    }

    private sealed class UnavailableTestVerifier : IFeature001IncidentReferenceVerifier
    {
        public Task<IncidentReferenceCheck> VerifyAsync(IncidentReference reference, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IncidentReferenceCheck(IncidentReferenceOutcome.Unavailable));
    }
}
