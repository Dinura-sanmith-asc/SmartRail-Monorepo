using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using HolyRice.Api.Persistence;

namespace HolyRice.Api.Orders;

public sealed record OrderLineRequest(Guid MenuItemId, int Quantity);
public sealed record CreateOrderRequest(Guid JourneyInstanceId, Guid CollectionStationId, List<OrderLineRequest>? Items);
public sealed record OrderLineResponse(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);
public sealed record OrderResponse(Guid Id, Guid JourneyInstanceId, Guid CollectionStationId, string Status,
    decimal Total, string Currency, DateTimeOffset CreatedAt, IReadOnlyList<OrderLineResponse> Items);
public sealed record MenuItemResponse(Guid Id, string Category, string Name, string Description,
    decimal Price, string Currency, bool Available, string? ImageUrl);
public sealed record CollectionPointResponse(Guid StationId, string StationName, string Instructions, bool Available);

public sealed class OrderService(HolyRiceDbContext db)
{
    public static OrderResponse ToResponse(CateringOrder order) => new(order.Id, order.JourneyInstanceId,
        order.CollectionStationId, order.Status, order.Total, order.Currency, order.CreatedAt,
        order.Items.Select(x => new OrderLineResponse(x.MenuItemId, x.Name, x.Quantity, x.UnitPrice)).ToArray());

    public async Task<(int Status, OrderResponse? Order, string? Error)> CreateAsync(
        CreateOrderRequest? request, string? key, CancellationToken ct)
    {
        if (request is null || request.JourneyInstanceId == Guid.Empty || request.CollectionStationId == Guid.Empty ||
            request.Items is not { Count: > 0 and <= 30 } || request.Items.Any(x => x.MenuItemId == Guid.Empty || x.Quantity is < 1 or > 20) ||
            request.Items.GroupBy(x => x.MenuItemId).Any(x => x.Count() > 1) ||
            string.IsNullOrWhiteSpace(key) || key.Length > 128)
            return (400, null, "Invalid order or idempotency key.");

        var canonical = JsonSerializer.Serialize(new
        {
            request.JourneyInstanceId, request.CollectionStationId,
            Items = request.Items.OrderBy(x => x.MenuItemId).Select(x => new { x.MenuItemId, x.Quantity })
        });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var prior = await db.IdempotencyRecords.AsNoTracking().Include(x => x.Order).ThenInclude(x => x.Items)
            .SingleOrDefaultAsync(x => x.Key == key, ct);
        if (prior is not null) return prior.RequestHash == hash
            ? (200, ToResponse(prior.Order), null) : (409, null, "Idempotency key was used for another request.");

        var point = await db.CollectionPoints.AsNoTracking().SingleOrDefaultAsync(x => x.StationId == request.CollectionStationId, ct);
        if (point is null || !point.Available) return (400, null, "Collection point is unavailable.");
        var ids = request.Items.Select(x => x.MenuItemId).ToArray();
        var menu = await db.MenuItems.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        if (menu.Count != ids.Length || menu.Values.Any(x => !x.Available))
            return (400, null, "One or more menu items are unavailable.");
        var order = new CateringOrder
        {
            Id = Guid.NewGuid(), JourneyInstanceId = request.JourneyInstanceId,
            CollectionStationId = request.CollectionStationId, CreatedAt = DateTimeOffset.UtcNow,
            Items = request.Items.Select(line => new CateringOrderItem
            {
                Id = Guid.NewGuid(), MenuItemId = line.MenuItemId, Name = menu[line.MenuItemId].Name,
                Quantity = line.Quantity, UnitPrice = menu[line.MenuItemId].Price
            }).ToList()
        };
        order.Total = order.Items.Sum(x => x.UnitPrice * x.Quantity);
        db.Orders.Add(order);
        db.IdempotencyRecords.Add(new IdempotencyRecord { Key = key, RequestHash = hash, Order = order });
        try
        {
            await db.SaveChangesAsync(ct);
            return (201, ToResponse(order), null);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            var existing = await db.IdempotencyRecords.AsNoTracking().Include(x => x.Order).ThenInclude(x => x.Items)
                .SingleOrDefaultAsync(x => x.Key == key, ct);
            return existing is not null && existing.RequestHash == hash
                ? (200, ToResponse(existing.Order), null) : (409, null, "Idempotency key was used for another request.");
        }
    }
}
