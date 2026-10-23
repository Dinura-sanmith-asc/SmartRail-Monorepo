using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class TrainServiceRouteStopConfiguration : IEntityTypeConfiguration<TrainServiceRouteStop>
{
    public void Configure(EntityTypeBuilder<TrainServiceRouteStop> builder)
    {
        builder.ToTable("train_service_route_stop", table => table.HasCheckConstraint("ck_train_service_route_stop_order_positive", "stop_order > 0"));
        builder.HasKey(stop => new { stop.ServiceId, stop.RouteStopId }).HasName("pk_train_service_route_stop");
        builder.Property(stop => stop.ServiceId).HasColumnName("service_id");
        builder.Property(stop => stop.RouteStopId).HasColumnName("route_stop_id");
        builder.Property(stop => stop.StopOrder).HasColumnName("stop_order").IsRequired();
        builder.HasOne<TrainService>().WithMany().HasForeignKey(stop => stop.ServiceId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_train_service_route_stop_service");
        builder.HasOne<RouteStop>().WithMany().HasForeignKey(stop => stop.RouteStopId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_train_service_route_stop_route_stop");
        builder.HasIndex(stop => stop.RouteStopId).HasDatabaseName("ix_train_service_route_stop_route_stop_id");
        builder.HasIndex(stop => new { stop.ServiceId, stop.StopOrder }).IsUnique().HasDatabaseName("ux_train_service_route_stop_order");
    }
}
