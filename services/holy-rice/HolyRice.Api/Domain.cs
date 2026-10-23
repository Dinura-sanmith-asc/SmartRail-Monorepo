namespace HolyRice.Api;

public sealed class MenuCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<MenuItem> Items { get; set; } = [];
}

public sealed class MenuItem
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public MenuCategory Category { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "LKR";
    public bool Available { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class CollectionPoint
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = "";
    public string Instructions { get; set; } = "";
    public bool Available { get; set; }
}

public sealed class CateringOrder
{
    public Guid Id { get; set; }
    public Guid JourneyInstanceId { get; set; }
    public Guid CollectionStationId { get; set; }
    public CollectionPoint CollectionPoint { get; set; } = null!;
    public string Status { get; set; } = "Placed";
    public decimal Total { get; set; }
    public string Currency { get; set; } = "LKR";
    public DateTimeOffset CreatedAt { get; set; }
    public List<CateringOrderItem> Items { get; set; } = [];
}

public sealed class CateringOrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public CateringOrder Order { get; set; } = null!;
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class IdempotencyRecord
{
    public string Key { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public Guid OrderId { get; set; }
    public CateringOrder Order { get; set; } = null!;
}
