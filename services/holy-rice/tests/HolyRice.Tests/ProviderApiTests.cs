using System.Net;
using System.Net.Http.Json;
using HolyRice.Api.Orders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HolyRice.Tests;

public sealed class ProviderApiTests : IAsyncLifetime
{
    private const string Key = "local-only-32-characters-long-secret-2026";
    private const string Station = "10000000-0000-0000-0000-000000000001";
    private const string Item = "d1415212-99cf-4acc-891e-54abb8f90b11";
    private readonly string _dbName = "holy_rice_test_" + Guid.NewGuid().ToString("N");
    private readonly string _admin = Environment.GetEnvironmentVariable("HOLY_RICE_TEST_ADMIN_DB") ??
        "Host=localhost;Port=5433;Database=postgres;Username=smartrail_test_admin;Password=local-integration-only";
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(_admin);
        await connection.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE DATABASE \"{_dbName}\"", connection))
            await command.ExecuteNonQueryAsync();
        var db = new NpgsqlConnectionStringBuilder(_admin) { Database = _dbName }.ConnectionString;
        Environment.SetEnvironmentVariable("HOLY_RICE_DB", db);
        Environment.SetEnvironmentVariable("HOLY_RICE_API_KEY", Key);
        Environment.SetEnvironmentVariable("HOLY_RICE_DEMO_STATIONS", Station + "|Colombo Fort");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HOLY_RICE_DB"] = db, ["HOLY_RICE_API_KEY"] = Key,
                ["HOLY_RICE_DEMO_STATIONS"] = Station + "|Colombo Fort"
            }));
        });
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Provider-Key", Key);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        Environment.SetEnvironmentVariable("HOLY_RICE_DB", null);
        Environment.SetEnvironmentVariable("HOLY_RICE_API_KEY", null);
        Environment.SetEnvironmentVariable("HOLY_RICE_DEMO_STATIONS", null);
        await using var connection = new NpgsqlConnection(_admin);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static CreateOrderRequest Valid() => new(Guid.NewGuid(), Guid.Parse(Station),
        [new(Guid.Parse(Item), 2)]);
    private async Task<HttpResponseMessage> Post(CreateOrderRequest body, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        request.Headers.Add("Idempotency-Key", key);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task Catalogue_and_collection_points_are_provider_owned()
    {
        var menu = await _client.GetFromJsonAsync<List<MenuItemResponse>>("/api/menu");
        Assert.Equal(4, menu!.Count);
        Assert.Contains(menu, x => !x.Available);
        var points = await _client.GetFromJsonAsync<List<CollectionPointResponse>>("/api/collection-points");
        Assert.Equal(Guid.Parse(Station), Assert.Single(points!).StationId);
    }

    [Fact]
    public async Task Requires_server_authentication()
    {
        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/menu")).StatusCode);
    }

    [Fact]
    public async Task Creates_order_with_server_price_and_progresses_status()
    {
        var response = await Post(Valid(), Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(1900, order!.Total);
        Assert.Equal("Placed", order.Status);
        var progressed = await _client.PostAsync($"/api/orders/{order.Id}/advance", null);
        Assert.Equal("Preparing", (await progressed.Content.ReadFromJsonAsync<OrderResponse>())!.Status);
        var read = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}");
        Assert.Equal("Preparing", read!.Status);
    }

    [Fact]
    public async Task Identical_retry_returns_original_and_conflict_is_rejected()
    {
        var request = Valid();
        var key = Guid.NewGuid().ToString();
        var first = await Post(request, key);
        var retry = await Post(request, key);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal((await first.Content.ReadFromJsonAsync<OrderResponse>())!.Id,
            (await retry.Content.ReadFromJsonAsync<OrderResponse>())!.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(request with { Items = [new(Guid.Parse(Item), 3)] }, key)).StatusCode);
    }

    [Fact]
    public async Task Concurrent_duplicate_submissions_create_one_order()
    {
        var request = Valid();
        var key = Guid.NewGuid().ToString();
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Post(request, key)));
        var orders = await Task.WhenAll(responses.Select(async response =>
            await response.Content.ReadFromJsonAsync<OrderResponse>()));
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        Assert.Single(orders.Select(x => x!.Id).Distinct());
    }

    [Fact]
    public async Task Invalid_quantity_item_and_point_are_rejected()
    {
        var request = Valid();
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(request with { Items = [new(Guid.Parse(Item), 0)] }, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(request with { Items = [new(Guid.NewGuid(), 1)] }, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(request with { CollectionStationId = Guid.NewGuid() }, Guid.NewGuid().ToString())).StatusCode);
    }
}
