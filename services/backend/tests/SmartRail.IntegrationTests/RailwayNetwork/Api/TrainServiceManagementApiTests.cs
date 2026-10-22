using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Domain.Identity;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.RailwayNetwork.Api;

public sealed class TrainServiceManagementApiTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
    : IClassFixture<PostgreSqlTestDatabaseFixture>, IClassFixture<LocalOidcFixture>
{
    [Fact]
    public async Task Controller_can_create_update_list_and_delete_train_services()
    {
        using var factory = await IdentityApiTestHost.CreateAsync(database, oidc, seedRailway: true);
        using var client = factory.CreateClient();
        var subject = "train-service-manager-" + Guid.NewGuid().ToString("N");
        await IdentityTestData.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var token = oidc.CreateToken(subject);

        using var routeRequest = IdentityTestData.Bearer(HttpMethod.Get, "/api/routes/management", token);
        using var routeResponse = await client.SendAsync(routeRequest);
        Assert.Equal(HttpStatusCode.OK, routeResponse.StatusCode);
        var routeItems = await routeResponse.Content.ReadFromJsonAsync<RouteManagementView[]>();
        var route = Assert.Single(routeItems!, item => item.RouteId == Guid.Parse("20000000-0000-0000-0000-000000000001"));

        using var create = IdentityTestData.Bearer(HttpMethod.Post, "/api/services", token);
        create.Content = JsonContent.Create(new CreateTrainServiceRequest("SRL-MGMT-TEST", route.RouteId, "Management Express", "TEST-804"));
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var service = await createResponse.Content.ReadFromJsonAsync<TrainServiceManagementView>();
        Assert.NotNull(service);
        Assert.NotEqual(Guid.Empty, service.ServiceId);

        using var duplicate = IdentityTestData.Bearer(HttpMethod.Post, "/api/services", token);
        duplicate.Content = JsonContent.Create(new CreateTrainServiceRequest("SRL-MGMT-TEST-2", route.RouteId, "Duplicate number", "TEST-804"));
        using var duplicateResponse = await client.SendAsync(duplicate);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var update = IdentityTestData.Bearer(HttpMethod.Patch, $"/api/services/{service.ServiceId:D}", token);
        update.Content = JsonContent.Create(new UpdateTrainServiceRequest("SRL-MGMT-TEST", route.RouteId, "Management Express Updated", "TEST-805"));
        using var updateResponse = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var list = IdentityTestData.Bearer(HttpMethod.Get, "/api/services/management", token);
        using var listResponse = await client.SendAsync(list);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listed = await listResponse.Content.ReadFromJsonAsync<TrainServiceManagementView[]>();
        Assert.Contains(listed!, item => item.ServiceId == service.ServiceId && item.DisplayName == "Management Express Updated");

        using var delete = IdentityTestData.Bearer(HttpMethod.Delete, $"/api/services/{service.ServiceId:D}", token);
        using var deleteResponse = await client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
