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
    public async Task Controller_can_manage_service_timetable_publication_dates_and_public_search_without_rewriting_history()
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

        using var scheduleRequest = IdentityTestData.Bearer(HttpMethod.Post, $"/api/services/{service.ServiceId:D}/schedules", token);
        scheduleRequest.Content = JsonContent.Create(new ScheduleRevisionInput(new TimeOnly(8, 0),
        [
            new CallingPointInput(route.Stops[0].RouteStopId, null, 0),
            new CallingPointInput(route.Stops[^1].RouteStopId, 60, null)
        ]));
        using var scheduleResponse = await client.SendAsync(scheduleRequest);
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);
        var schedule = await scheduleResponse.Content.ReadFromJsonAsync<ScheduleRevisionView>();
        Assert.NotNull(schedule);

        using var publish = IdentityTestData.Bearer(HttpMethod.Post,
            $"/api/services/{service.ServiceId:D}/schedules/{schedule.ScheduleId:D}/publish", token);
        using var publishResponse = await client.SendAsync(publish);
        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);

        var operatingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var dateKey = operatingDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        using var manifest = IdentityTestData.Bearer(HttpMethod.Put, $"/api/service-date-manifests/{dateKey}", token);
        manifest.Content = JsonContent.Create(new
        {
            sourceRevision = "management-test-source",
            fixtureVersion = "management-test-v1",
            isComplete = true,
            contentHash = "sha256:management-test",
            scheduleIds = new[] { schedule.ScheduleId }
        });
        using var manifestResponse = await client.SendAsync(manifest);
        Assert.Equal(HttpStatusCode.OK, manifestResponse.StatusCode);

        using var operationDates = IdentityTestData.Bearer(HttpMethod.Put,
            $"/api/services/{service.ServiceId:D}/schedules/{schedule.ScheduleId:D}/operation-dates", token);
        operationDates.Content = JsonContent.Create(new { dates = new[] { operatingDate } });
        using var datesResponse = await client.SendAsync(operationDates);
        Assert.Equal(HttpStatusCode.OK, datesResponse.StatusCode);

        using var detail = await client.GetAsync($"/api/services/{service.ServiceId:D}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal("Management Express Updated", detailJson.RootElement.GetProperty("displayName").GetString());
        Assert.Equal("TEST-805", detailJson.RootElement.GetProperty("publicServiceNumber").GetString());
        using var search = await client.GetAsync($"/api/services?origin={route.Stops[0].StationId:D}&destination={route.Stops[^1].StationId:D}&date={dateKey}");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        using var searchJson = JsonDocument.Parse(await search.Content.ReadAsStringAsync());
        var found = Assert.Single(searchJson.RootElement.GetProperty("services").EnumerateArray(), item => item.GetProperty("serviceId").GetGuid() == service.ServiceId);
        Assert.Equal("Management Express Updated", found.GetProperty("displayName").GetString());
        Assert.Equal("TEST-805", found.GetProperty("publicServiceNumber").GetString());

        using var revisions = IdentityTestData.Bearer(HttpMethod.Get, $"/api/services/{service.ServiceId:D}/schedules", token);
        using var revisionsResponse = await client.SendAsync(revisions);
        var history = await revisionsResponse.Content.ReadFromJsonAsync<ScheduleRevisionView[]>();
        var publishedHistory = Assert.Single(history!);
        Assert.Equal(schedule.ScheduleId, publishedHistory.ScheduleId);
        Assert.Contains(publishedHistory.OperationDates, item => item.OperationDate == operatingDate && item.IsManifestComplete);

        using var unsafeDelete = IdentityTestData.Bearer(HttpMethod.Delete, $"/api/services/{service.ServiceId:D}", token);
        using var unsafeDeleteResponse = await client.SendAsync(unsafeDelete);
        Assert.Equal(HttpStatusCode.Conflict, unsafeDeleteResponse.StatusCode);

        using var createDisposable = IdentityTestData.Bearer(HttpMethod.Post, "/api/services", token);
        createDisposable.Content = JsonContent.Create(new CreateTrainServiceRequest("SRL-MGMT-DISPOSABLE", route.RouteId, null, "TEST-806"));
        using var disposableResponse = await client.SendAsync(createDisposable);
        var disposable = await disposableResponse.Content.ReadFromJsonAsync<TrainServiceManagementView>();
        using var deleteDisposable = IdentityTestData.Bearer(HttpMethod.Delete, $"/api/services/{disposable!.ServiceId:D}", token);
        using var deleteResponse = await client.SendAsync(deleteDisposable);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
