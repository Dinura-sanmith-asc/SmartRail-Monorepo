using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class ServiceCallingPointConfiguration : IEntityTypeConfiguration<ServiceCallingPoint>
{
    public void Configure(EntityTypeBuilder<ServiceCallingPoint> builder)
    {
        builder.ToTable("service_calling_point", table =>
        {
            table.HasCheckConstraint("ck_calling_point_arrival_nonnegative", "arrival_offset_minutes IS NULL OR arrival_offset_minutes >= 0");
            table.HasCheckConstraint("ck_calling_point_departure_nonnegative", "departure_offset_minutes IS NULL OR departure_offset_minutes >= 0");
            table.HasCheckConstraint("ck_calling_point_arrival_before_departure", "arrival_offset_minutes IS NULL OR departure_offset_minutes IS NULL OR arrival_offset_minutes <= departure_offset_minutes");
        });
        builder.HasKey(call => new { call.ScheduleId, call.RouteStopId }).HasName("pk_service_calling_point");
        builder.Property(call => call.ScheduleId).HasColumnName("schedule_id");
        builder.Property(call => call.RouteStopId).HasColumnName("route_stop_id");
        builder.Property(call => call.ArrivalOffsetMinutes).HasColumnName("arrival_offset_minutes");
        builder.Property(call => call.DepartureOffsetMinutes).HasColumnName("departure_offset_minutes");
        builder.HasOne<ServiceScheduleRevision>().WithMany().HasForeignKey(call => call.ScheduleId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_calling_point_schedule");
        builder.HasOne<RouteStop>().WithMany().HasForeignKey(call => call.RouteStopId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_calling_point_route_stop");
        builder.HasIndex(call => call.RouteStopId).HasDatabaseName("ix_calling_point_origin_destination");
    }
}