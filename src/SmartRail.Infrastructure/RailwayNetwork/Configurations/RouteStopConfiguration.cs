using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("route_stop", table => table.HasCheckConstraint("ck_route_stop_order_positive", "stop_order > 0"));
        builder.HasKey(stop => stop.RouteStopId).HasName("pk_route_stop");
        builder.Property(stop => stop.RouteStopId).HasColumnName("route_stop_id").ValueGeneratedNever();
        builder.Property(stop => stop.RouteId).HasColumnName("route_id").IsRequired();
        builder.Property(stop => stop.StationId).HasColumnName("station_id").IsRequired();
        builder.Property(stop => stop.StopOrder).HasColumnName("stop_order").IsRequired();
        builder.HasOne<Route>().WithMany().HasForeignKey(stop => stop.RouteId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_route_stop_route");
        builder.HasOne<Station>().WithMany().HasForeignKey(stop => stop.StationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_route_stop_station");
        builder.HasIndex(stop => new { stop.RouteId, stop.StopOrder }).IsUnique().HasDatabaseName("ux_route_stop_route_order");
    }
}