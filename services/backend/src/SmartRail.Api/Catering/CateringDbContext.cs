using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartRail.Api.Catering;

public sealed class CateringCorrelation
{
    public Guid Id { get; set; }
    public string PassengerSubject { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public Guid JourneyInstanceId { get; set; }
    public Guid CollectionStationId { get; set; }
    public Guid? ProviderOrderId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CateringDbContext(DbContextOptions<CateringDbContext> options) : DbContext(options)
{
    public DbSet<CateringCorrelation> Correlations => Set<CateringCorrelation>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<CateringCorrelation>(e =>
        {
            e.ToTable("catering_order_correlations");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PassengerSubject, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => x.ProviderOrderId).IsUnique();
            e.Property(x => x.PassengerSubject).HasMaxLength(200).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            e.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        });
    }
}

public sealed class CateringDbContextFactory : IDesignTimeDbContextFactory<CateringDbContext>
{
    public CateringDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<CateringDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__SmartRailDbContext") ??
            "Host=localhost;Database=smartrail_dev;Username=smartrail_dev;Password=local-development-only").Options);
}
