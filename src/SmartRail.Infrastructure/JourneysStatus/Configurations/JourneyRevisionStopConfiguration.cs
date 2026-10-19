using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.JourneysStatus.Configurations;

public sealed class JourneyRevisionStopConfiguration : IEntityTypeConfiguration<JourneyRevisionStop>
{
    public void Configure(EntityTypeBuilder<JourneyRevisionStop> builder)
    {
        builder.ToTable("journey_revision_stop", table =>
        {
            table.HasCheckConstraint("ck_journey_stop_order_positive", "stop_order > 0");
            table.HasCheckConstraint("ck_journey_stop_ambiguity", "ambiguity_state IN ('none','ambiguous')");
            table.HasCheckConstraint("ck_journey_stop_platform_not_blank", "platform IS NULL OR length(trim(platform)) > 0");
        });
        builder.HasKey(x => new { x.JourneyInstanceId, x.Revision, x.JourneyStopId }).HasName("pk_journey_revision_stop");
        builder.Property(x => x.JourneyInstanceId).HasColumnName("journey_instance_id");
        builder.Property(x => x.Revision).HasColumnName("revision");
        builder.Property(x => x.JourneyStopId).HasColumnName("journey_stop_id").ValueGeneratedNever();
        builder.Property(x => x.StopOrder).HasColumnName("stop_order").IsRequired();
        builder.Property(x => x.StationId).HasColumnName("station_id").IsRequired();
        builder.Property(x => x.RouteStopId).HasColumnName("route_stop_id");
        builder.Property(x => x.PlannedArrivalAt).HasColumnName("planned_arrival_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.PlannedDepartureAt).HasColumnName("planned_departure_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.OperationalArrivalAt).HasColumnName("operational_arrival_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.OperationalDepartureAt).HasColumnName("operational_departure_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Platform).HasColumnName("platform").HasMaxLength(100);
        builder.Property(x => x.TimingSource).HasColumnName("timing_source").HasMaxLength(30);
        builder.Property(x => x.TimingUpdatedAt).HasColumnName("timing_updated_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.AmbiguityState).HasColumnName("ambiguity_state").HasMaxLength(20).IsRequired();
        builder.HasOne<JourneyRevision>().WithMany().HasForeignKey(x => new { x.JourneyInstanceId, x.Revision }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_journey_stop_revision");
        builder.HasOne<Station>().WithMany().HasForeignKey(x => x.StationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_journey_stop_station");
        builder.HasOne<RouteStop>().WithMany().HasForeignKey(x => x.RouteStopId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_journey_stop_route_stop");
        builder.HasIndex(x => new { x.JourneyInstanceId, x.Revision, x.StopOrder }).IsUnique().HasDatabaseName("ux_journey_stop_revision_order");
        builder.HasIndex(x => x.StationId).HasDatabaseName("ix_journey_stop_station");
    }
}
