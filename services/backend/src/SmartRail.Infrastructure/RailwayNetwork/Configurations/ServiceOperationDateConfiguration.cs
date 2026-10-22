using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class ServiceOperationDateConfiguration : IEntityTypeConfiguration<ServiceOperationDate>
{
    public void Configure(EntityTypeBuilder<ServiceOperationDate> builder)
    {
        builder.ToTable("service_operation_date");
        builder.HasKey(operation => new { operation.ScheduleId, operation.OperationDate }).HasName("pk_service_operation_date");
        builder.Property(operation => operation.ScheduleId).HasColumnName("schedule_id");
        builder.Property(operation => operation.OperationDate).HasColumnName("operation_date").HasColumnType("date");
        builder.HasOne<ServiceScheduleRevision>().WithMany().HasForeignKey(operation => operation.ScheduleId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_service_operation_schedule");
        builder.HasOne<ServiceDateManifest>().WithMany().HasForeignKey(operation => operation.OperationDate).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_service_operation_manifest");
        builder.HasIndex(operation => new { operation.OperationDate, operation.ScheduleId }).HasDatabaseName("ix_service_operation_date_schedule");
    }
}