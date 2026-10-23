using System.Security.Cryptography;
using System.Text;
using HolyRice.Api;
using HolyRice.Api.Orders;
using HolyRice.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration["HOLY_RICE_DB"] ?? throw new InvalidOperationException("HOLY_RICE_DB is required.");
var apiKey = builder.Configuration["HOLY_RICE_API_KEY"];
if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 32)
    throw new InvalidOperationException("HOLY_RICE_API_KEY must contain at least 32 characters.");
builder.Services.AddDbContext<HolyRiceDbContext>(options => options.UseNpgsql(connection));
builder.Services.AddScoped<OrderService>();
builder.Services.AddHealthChecks();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HolyRiceDbContext>();
    await db.Database.MigrateAsync();
    await DevelopmentSeed.SeedAsync(db, app.Configuration);
}
app.MapHealthChecks("/health");
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health") { await next(); return; }
    var supplied = context.Request.Headers["X-Provider-Key"].ToString();
    var expected = Encoding.UTF8.GetBytes(apiKey);
    var actual = Encoding.UTF8.GetBytes(supplied);
    if (actual.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(actual, expected))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Provider authentication required." });
        return;
    }
    await next();
});

app.MapGet("/api/menu", async (HolyRiceDbContext db, CancellationToken ct) =>
    await db.MenuItems.AsNoTracking().Include(x => x.Category).OrderBy(x => x.Name)
        .Select(x => new MenuItemResponse(x.Id, x.Category.Name, x.Name, x.Description,
            x.Price, x.Currency, x.Available, x.ImageUrl)).ToListAsync(ct));
app.MapGet("/api/menu/{id:guid}", async (Guid id, HolyRiceDbContext db, CancellationToken ct) =>
{
    var item = await db.MenuItems.AsNoTracking().Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id, ct);
    return item is null ? Results.NotFound() : Results.Ok(new MenuItemResponse(item.Id, item.Category.Name,
        item.Name, item.Description, item.Price, item.Currency, item.Available, item.ImageUrl));
});
app.MapGet("/api/collection-points", async (HolyRiceDbContext db, CancellationToken ct) =>
    await db.CollectionPoints.AsNoTracking().OrderBy(x => x.StationName)
        .Select(x => new CollectionPointResponse(x.StationId, x.StationName, x.Instructions, x.Available)).ToListAsync(ct));
app.MapPost("/api/orders", async (CreateOrderRequest? request, HttpContext http, OrderService orders, CancellationToken ct) =>
{
    var result = await orders.CreateAsync(request, http.Request.Headers["Idempotency-Key"].ToString(), ct);
    return result.Order is not null ? Results.Json(result.Order, statusCode: result.Status)
        : Results.Problem(result.Error, statusCode: result.Status);
});
app.MapGet("/api/orders/{id:guid}", async (Guid id, HolyRiceDbContext db, CancellationToken ct) =>
{
    var order = await db.Orders.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
    return order is null ? Results.NotFound() : Results.Ok(OrderService.ToResponse(order));
});
// Local provider operations allow a demonstrator to progress the provider-owned state.
if (app.Environment.IsDevelopment())
    app.MapPost("/api/orders/{id:guid}/advance", async (Guid id, HolyRiceDbContext db, CancellationToken ct) =>
    {
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return Results.NotFound();
        order.Status = order.Status switch { "Placed" => "Preparing", "Preparing" => "Ready", "Ready" => "Collected", _ => order.Status };
        await db.SaveChangesAsync(ct);
        return Results.Ok(OrderService.ToResponse(order));
    });
app.Run();
public partial class Program { }
