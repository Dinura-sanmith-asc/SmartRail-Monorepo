using Microsoft.EntityFrameworkCore;

namespace HolyRice.Api.Persistence;

public sealed class HolyRiceDbContext(DbContextOptions<HolyRiceDbContext> options) : DbContext(options)
{
    public DbSet<MenuCategory> Categories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<CollectionPoint> CollectionPoints => Set<CollectionPoint>();
    public DbSet<CateringOrder> Orders => Set<CateringOrder>();
    public DbSet<CateringOrderItem> OrderItems => Set<CateringOrderItem>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<MenuCategory>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(100).IsRequired(); });
        model.Entity<MenuItem>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(120).IsRequired(); e.Property(x => x.Description).HasMaxLength(500); e.Property(x => x.Currency).HasMaxLength(3).IsRequired(); e.Property(x => x.Price).HasPrecision(12, 2); e.ToTable(t => t.HasCheckConstraint("ck_menu_price", "\"Price\" >= 0")); });
        model.Entity<CollectionPoint>(e => { e.HasKey(x => x.StationId); e.Property(x => x.StationName).HasMaxLength(120).IsRequired(); e.Property(x => x.Instructions).HasMaxLength(300).IsRequired(); });
        model.Entity<CateringOrder>(e => { e.HasKey(x => x.Id); e.Property(x => x.Status).HasMaxLength(24).IsRequired(); e.Property(x => x.Currency).HasMaxLength(3).IsRequired(); e.Property(x => x.Total).HasPrecision(12, 2); e.HasOne(x => x.CollectionPoint).WithMany().HasForeignKey(x => x.CollectionStationId).OnDelete(DeleteBehavior.Restrict); });
        model.Entity<CateringOrderItem>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(120).IsRequired(); e.Property(x => x.UnitPrice).HasPrecision(12, 2); e.ToTable(t => t.HasCheckConstraint("ck_order_quantity", "\"Quantity\" > 0 AND \"Quantity\" <= 20")); });
        model.Entity<IdempotencyRecord>(e => { e.HasKey(x => x.Key); e.Property(x => x.Key).HasMaxLength(128); e.Property(x => x.RequestHash).HasMaxLength(64).IsRequired(); e.HasOne(x => x.Order).WithOne().HasForeignKey<IdempotencyRecord>(x => x.OrderId).OnDelete(DeleteBehavior.Restrict); });
    }
}
