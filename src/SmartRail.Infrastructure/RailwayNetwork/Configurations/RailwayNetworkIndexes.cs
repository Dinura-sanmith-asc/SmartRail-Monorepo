using Microsoft.EntityFrameworkCore;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public static class RailwayNetworkIndexes
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RouteStop>()
            .HasIndex(stop => new { stop.StationId, stop.RouteId })
            .HasDatabaseName("ix_route_stop_station_route");
    }
}