using Microsoft.EntityFrameworkCore;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork.Configurations;

namespace SmartRail.Infrastructure.RailwayNetwork;

public sealed class SmartRailDbContext(DbContextOptions<SmartRailDbContext> options) : DbContext(options)
{
    public const string ConnectionStringName = "SmartRailDbContext";

    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<TrainService> TrainServices => Set<TrainService>();
    public DbSet<ServiceScheduleRevision> ServiceScheduleRevisions => Set<ServiceScheduleRevision>();
    public DbSet<ServiceCallingPoint> ServiceCallingPoints => Set<ServiceCallingPoint>();
    public DbSet<ServiceDateManifest> ServiceDateManifests => Set<ServiceDateManifest>();
    public DbSet<ServiceOperationDate> ServiceOperationDates => Set<ServiceOperationDate>();
    public DbSet<StationImportRun> StationImportRuns => Set<StationImportRun>();
    public DbSet<StationChange> StationChanges => Set<StationChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartRailDbContext).Assembly);
        RailwayNetworkIndexes.Configure(modelBuilder);
    }
}