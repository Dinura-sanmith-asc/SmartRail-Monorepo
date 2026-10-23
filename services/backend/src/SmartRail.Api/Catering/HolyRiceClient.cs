using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartRail.Api.Catering;

public sealed record MenuItemDto(Guid Id, string Category, string Name, string Description,
    decimal Price, string Currency, bool Available, string? ImageUrl);
public sealed record CollectionPointDto(Guid StationId, string StationName, string Instructions, bool Available);
public sealed record OrderItemDto(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);
public sealed record ProviderOrderDto(Guid Id, Guid JourneyInstanceId, Guid CollectionStationId,
    string Status, decimal Total, string Currency, DateTimeOffset CreatedAt, IReadOnlyList<OrderItemDto> Items);
public sealed record CreateProviderOrderDto(Guid JourneyInstanceId, Guid CollectionStationId,
    IReadOnlyList<CreateOrderItemDto> Items);
public sealed record CreateOrderItemDto(Guid MenuItemId, int Quantity);

public sealed class HolyRiceClient(HttpClient http, IConfiguration configuration)
{
    private readonly string _key = configuration["HolyRice:ApiKey"] ?? "";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Provider-Key", _key);
        return request;
    }

    public async Task<IReadOnlyList<MenuItemDto>> MenuAsync(CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, "/api/menu");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MenuItemDto>>(Json, ct) ?? throw new JsonException();
    }

    public async Task<IReadOnlyList<CollectionPointDto>> PointsAsync(CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, "/api/collection-points");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CollectionPointDto>>(Json, ct) ?? throw new JsonException();
    }

    public async Task<ProviderOrderDto?> OrderAsync(Guid id, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, $"/api/orders/{id}");
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProviderOrderDto>(Json, ct) ?? throw new JsonException();
    }

    public async Task<(HttpStatusCode Status, ProviderOrderDto? Order)> CreateAsync(CreateProviderOrderDto body,
        string key, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Post, "/api/orders");
        request.Headers.Add("Idempotency-Key", key);
        request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return (response.StatusCode, null);
        return (response.StatusCode, await response.Content.ReadFromJsonAsync<ProviderOrderDto>(Json, ct));
    }
}
