using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartRail.Api.Authorization;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Domain.Identity;

namespace SmartRail.Api.Catering;

public sealed record CateringOptionsResponse(Guid JourneyInstanceId, string Provider,
    IReadOnlyList<MenuItemDto> Items, IReadOnlyList<CollectionPointDto> CollectionPoints);
public sealed record CreateCateringOrderRequest(Guid JourneyInstanceId, Guid CollectionStationId,
    IReadOnlyList<CreateOrderItemDto>? Items);
public sealed record CateringOrderResponse(Guid Id, Guid JourneyInstanceId, Guid CollectionStationId,
    string Status, decimal? Total, string Currency, DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items);

public static class CateringEndpoints
{
    public static IEndpointRouteBuilder MapCateringEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/catering/options", Options).AllowAnonymous().WithTags("Catering");
        endpoints.MapPost("/api/catering/orders", Create).RequireAuthorization().WithTags("Catering");
        endpoints.MapGet("/api/catering/orders", List).RequireAuthorization().WithTags("Catering");
        endpoints.MapGet("/api/catering/orders/{id:guid}", Get).RequireAuthorization().WithTags("Catering");
        return endpoints;
    }

    private static async Task<IResult> Options(Guid journeyId, GetJourneyHandler journeyReader,
        HolyRiceClient provider, CancellationToken ct)
    {
        if (journeyId == Guid.Empty) return Results.Problem("A dated journey is required.", statusCode: 400);
        try
        {
            var journey = await journeyReader.HandleAsync(new(journeyId), ct);
            if (journey.Outcome == JourneyReadOutcome.NotFound) return Results.NotFound();
            if (journey.Outcome != JourneyReadOutcome.Found || journey.Value is null) return Unavailable();
            var menu = await provider.MenuAsync(ct);
            var points = await provider.PointsAsync(ct);
            var stops = journey.Value.Stops.Select(x => x.StationId).ToHashSet();
            return Results.Ok(new CateringOptionsResponse(journeyId, "Holy-Rice", menu,
                points.Where(x => x.Available && stops.Contains(x.StationId)).ToArray()));
        }
        catch (Exception ex) when (ProviderFailure(ex)) { return Unavailable(); }
    }

    private static async Task<IResult> Create(CreateCateringOrderRequest? body, HttpContext context,
        IAuthorizationService authorization, GetJourneyHandler journeyReader, HolyRiceClient provider,
        CateringDbContext db, CancellationToken ct)
    {
        var denied = await Authorize(context, authorization, PermissionAction.CreateCateringOrder, ProtectedResource.None);
        if (denied is not null) return denied;
        var key = context.Request.Headers["Idempotency-Key"].ToString();
        if (body is null || body.JourneyInstanceId == Guid.Empty || body.CollectionStationId == Guid.Empty ||
            body.Items is not { Count: > 0 and <= 30 } || body.Items.Any(x => x.MenuItemId == Guid.Empty || x.Quantity is < 1 or > 20) ||
            body.Items.GroupBy(x => x.MenuItemId).Any(x => x.Count() > 1) || string.IsNullOrWhiteSpace(key) || key.Length > 128)
            return Results.Problem("Invalid order or idempotency key.", statusCode: 400);
        var subject = context.User.FindFirstValue("sub")!;
        var canonical = JsonSerializer.Serialize(new { body.JourneyInstanceId, body.CollectionStationId,
            Items = body.Items.OrderBy(x => x.MenuItemId).Select(x => new { x.MenuItemId, x.Quantity }) });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        CateringCorrelation? record;
        try
        {
            record = await db.Correlations.SingleOrDefaultAsync(x => x.PassengerSubject == subject && x.IdempotencyKey == key, ct);
            if (record is not null && record.RequestHash != hash)
                return Results.Problem("Idempotency key was used for another order.", statusCode: 409);
            if (record is null)
            {
                var journey = await journeyReader.HandleAsync(new(body.JourneyInstanceId), ct);
                if (journey.Outcome == JourneyReadOutcome.NotFound) return Results.NotFound();
                if (journey.Outcome != JourneyReadOutcome.Found || journey.Value is null) return Unavailable();
                var stations = journey.Value.Stops.Select(x => x.StationId).ToHashSet();
                var points = await provider.PointsAsync(ct);
                if (!stations.Contains(body.CollectionStationId) ||
                    !points.Any(x => x.StationId == body.CollectionStationId && x.Available))
                    return Results.Problem("Collection point is unavailable for this journey.", statusCode: 400);
                record = new CateringCorrelation { Id = Guid.NewGuid(), PassengerSubject = subject,
                    IdempotencyKey = key, RequestHash = hash, JourneyInstanceId = body.JourneyInstanceId,
                    CollectionStationId = body.CollectionStationId, CreatedAt = DateTimeOffset.UtcNow };
                db.Correlations.Add(record);
                try { await db.SaveChangesAsync(ct); }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                    record = await db.Correlations.SingleOrDefaultAsync(x => x.PassengerSubject == subject && x.IdempotencyKey == key, ct);
                    if (record is null) return Unavailable();
                    if (record.RequestHash != hash) return Results.Problem("Idempotency key was used for another order.", statusCode: 409);
                }
            }
            // The provider key is stable across process crashes and uncertain HTTP outcomes.
            var providerKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(subject + ":" + key)));
            var providerOrder = record.ProviderOrderId is Guid id
                ? await provider.OrderAsync(id, ct)
                : (await provider.CreateAsync(new(body.JourneyInstanceId, body.CollectionStationId, body.Items), providerKey, ct)).Order;
            if (providerOrder is null) return Results.Problem("Provider could not accept this order.", statusCode: 503);
            if (providerOrder.JourneyInstanceId != body.JourneyInstanceId || providerOrder.CollectionStationId != body.CollectionStationId)
                return Unavailable();
            if (record.ProviderOrderId is null)
            {
                record.ProviderOrderId = providerOrder.Id;
                await db.SaveChangesAsync(ct);
            }
            return Results.Json(Map(record, providerOrder), statusCode: 201);
        }
        catch (Exception ex) when (ProviderFailure(ex)) { return Unavailable(); }
    }

    private static async Task<IResult> Get(Guid id, HttpContext context, IAuthorizationService authorization,
        CateringDbContext db, HolyRiceClient provider, CancellationToken ct)
    {
        var denied = await Authorize(context, authorization, PermissionAction.ViewOwnCateringOrder,
            ProtectedResource.CateringOrder(id));
        if (denied is not null) return denied;
        try
        {
            var record = await db.Correlations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (record?.ProviderOrderId is not Guid providerId)
                return Results.Problem("Order result is pending. Retry with the original idempotency key.", statusCode: 503);
            var order = await provider.OrderAsync(providerId, ct);
            return order is null ? Unavailable() : Results.Ok(Map(record, order));
        }
        catch (Exception ex) when (ProviderFailure(ex)) { return Unavailable(); }
    }

    private static async Task<IResult> List(HttpContext context, IAuthorizationService authorization,
        CateringDbContext db, HolyRiceClient provider, CancellationToken ct)
    {
        var denied = await Authorize(context, authorization, PermissionAction.CreateCateringOrder, ProtectedResource.None);
        if (denied is not null) return denied;
        try
        {
            var subject = context.User.FindFirstValue("sub")!;
            var rows = await db.Correlations.AsNoTracking().Where(x => x.PassengerSubject == subject && x.ProviderOrderId != null)
                .OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
            var orders = new List<CateringOrderResponse>();
            foreach (var row in rows)
            {
                var order = await provider.OrderAsync(row.ProviderOrderId!.Value, ct);
                if (order is null) return Unavailable();
                orders.Add(Map(row, order));
            }
            return Results.Ok(orders);
        }
        catch (Exception ex) when (ProviderFailure(ex)) { return Unavailable(); }
    }

    private static CateringOrderResponse Map(CateringCorrelation row, ProviderOrderDto order)
    {
        if (order.Id == Guid.Empty || order.JourneyInstanceId != row.JourneyInstanceId ||
            order.CollectionStationId != row.CollectionStationId ||
            order.Status is not ("Placed" or "Preparing" or "Ready" or "Collected") ||
            order.Total < 0 || order.Currency != "LKR" || order.Items is null)
            throw new JsonException("Provider order response is invalid.");
        return new(row.Id, row.JourneyInstanceId, row.CollectionStationId, order.Status,
            order.Total, order.Currency, order.CreatedAt, order.Items);
    }
    private static IResult Unavailable() => Results.Problem("Catering is temporarily unavailable. Retry safely with the same key.", statusCode: 503);
    private static bool ProviderFailure(Exception ex) => ex is HttpRequestException or TaskCanceledException or
        JsonException or InvalidOperationException or DbUpdateException;
    private static async Task<IResult?> Authorize(HttpContext context, IAuthorizationService service,
        PermissionAction action, ProtectedResource resource)
    {
        var result = await service.AuthorizeAsync(context.User, null,
            new SmartRailAuthorizationRequirement(action, resource));
        if (result.Succeeded) return null;
        var unavailable = result.Failure?.FailureReasons.Any(x => x.Message == "smartrail_authorization_unavailable") == true;
        return Results.Problem(unavailable ? "Authorization is temporarily unavailable." : "Permission is denied.",
            statusCode: unavailable ? 503 : 403);
    }
}
