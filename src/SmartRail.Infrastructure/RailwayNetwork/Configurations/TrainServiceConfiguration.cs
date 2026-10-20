using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class TrainServiceConfiguration : IEntityTypeConfiguration<TrainService>
{
    public void Configure(EntityTypeBuilder<TrainService> builder)
    {
        builder.ToTable("train_service", table => table.HasCheckConstraint("ck_train_service_version_positive", "version > 0"));
        builder.HasKey(service => service.ServiceId).HasName("pk_train_service");
        builder.Property(service => service.ServiceId).HasColumnName("service_id").ValueGeneratedNever();
        builder.Property(service => service.ServiceCode).HasColumnName("service_code").IsRequired();
        builder.Property(service => service.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(service => service.PublicServiceNumber).HasColumnName("public_service_number").HasMaxLength(50);
        builder.Property(service => service.RouteId).HasColumnName("route_id").IsRequired();
        builder.Property(service => service.IsPublished).HasColumnName("is_published").IsRequired();
        builder.Property(service => service.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.HasOne<Route>().WithMany().HasForeignKey(service => service.RouteId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_train_service_route");
        builder.HasIndex(service => service.ServiceCode).IsUnique().HasDatabaseName("ux_train_service_service_code");
    }
}
