using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("route", table => table.HasCheckConstraint("ck_route_version_positive", "version > 0"));
        builder.HasKey(route => route.RouteId).HasName("pk_route");
        builder.Property(route => route.RouteId).HasColumnName("route_id").ValueGeneratedNever();
        builder.Property(route => route.RouteCode).HasColumnName("route_code").IsRequired();
        builder.Property(route => route.Name).HasColumnName("name").IsRequired();
        builder.Property(route => route.IsPublished).HasColumnName("is_published").IsRequired();
        builder.Property(route => route.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.HasIndex(route => route.RouteCode).IsUnique().HasDatabaseName("ux_route_route_code");
    }
}