using Microsoft.EntityFrameworkCore;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork.Configurations;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.Incidents.Entities;

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
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<RoleAssignmentAudit> RoleAssignmentAudits => Set<RoleAssignmentAudit>();
    public DbSet<StationScopeAssignment> StationScopeAssignments => Set<StationScopeAssignment>();
    public DbSet<StationScopeAssignmentAudit> StationScopeAssignmentAudits => Set<StationScopeAssignmentAudit>();
    public DbSet<Journey> Journeys => Set<Journey>();
    public DbSet<JourneyRevision> JourneyRevisions => Set<JourneyRevision>();
    public DbSet<JourneyRevisionStop> JourneyRevisionStops => Set<JourneyRevisionStop>();
    public DbSet<JourneyInitializationAudit> JourneyInitializationAudits => Set<JourneyInitializationAudit>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentRevision> IncidentRevisions => Set<IncidentRevision>();
    public DbSet<IncidentAssociation> IncidentAssociations => Set<IncidentAssociation>();
    public DbSet<IncidentIdempotencyRecord> IncidentIdempotencyRecords => Set<IncidentIdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartRailDbContext).Assembly);
        RailwayNetworkIndexes.Configure(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectAuditMutation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RejectAuditMutation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RejectAuditMutation()
    {
        var mutation = ChangeTracker.Entries()
            .Where(entry => entry.Entity is RoleAssignmentAudit or StationScopeAssignmentAudit or JourneyInitializationAudit or JourneyRevision)
            .FirstOrDefault(entry => entry.State is EntityState.Modified or EntityState.Deleted);
        if (mutation is not null)
        {
            throw new InvalidOperationException("Identity audit events are append-only and cannot be updated or deleted.");
        }
    }
}
