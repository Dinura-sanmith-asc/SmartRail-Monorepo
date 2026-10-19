using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.JourneysStatus.Entities;

namespace SmartRail.Infrastructure.JourneysStatus.Configurations;

public sealed class JourneyConfiguration : IEntityTypeConfiguration<Journey>
{
    public void Configure(EntityTypeBuilder<Journey> builder)
    {
        builder.ToTable("journey", table =>
        {
            table.HasCheckConstraint("ck_journey_schedule_revision_positive", "schedule_revision > 0");
            table.HasCheckConstraint("ck_journey_current_revision_positive", "current_revision > 0");
            table.HasCheckConstraint("ck_journey_required_text", "length(trim(source_system)) > 0 AND length(trim(source_instance_key)) > 0 AND length(trim(initialized_by_subject)) > 0 AND length(trim(initialization_provenance)) > 0");
        });
        builder.HasKey(x => x.JourneyInstanceId).HasName("pk_journey");
        builder.Property(x => x.JourneyInstanceId).HasColumnName("journey_instance_id").ValueGeneratedNever();
        builder.Property(x => x.ServiceId).HasColumnName("service_id").IsRequired();
        builder.Property(x => x.ScheduleId).HasColumnName("schedule_id").IsRequired();
        builder.Property(x => x.ScheduleRevision).HasColumnName("schedule_revision").IsRequired();
        builder.Property(x => x.OperationDate).HasColumnName("operation_date").HasColumnType("date").IsRequired();
        builder.Property(x => x.FirstDepartureOccurrence).HasColumnName("first_departure_occurrence").HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.SourceSystem).HasColumnName("source_system").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceInstanceKey).HasColumnName("source_instance_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.InitializedAt).HasColumnName("initialized_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.InitializedBySubject).HasColumnName("initialized_by_subject").HasMaxLength(300).IsRequired();
        builder.Property(x => x.InitializationProvenance).HasColumnName("initialization_provenance").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CurrentRevision).HasColumnName("current_revision").IsConcurrencyToken().IsRequired();
        builder.HasOne<SmartRail.Domain.RailwayNetwork.Entities.TrainService>().WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_journey_service");
        builder.HasOne<SmartRail.Domain.RailwayNetwork.Entities.ServiceScheduleRevision>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_journey_schedule");
        builder.HasIndex(x => new { x.SourceSystem, x.SourceInstanceKey }).IsUnique().HasDatabaseName("ux_journey_source_key");
        builder.HasIndex(x => new { x.ServiceId, x.OperationDate }).HasDatabaseName("ix_journey_service_operation_date");
        builder.HasIndex(x => new { x.ScheduleId, x.OperationDate }).HasDatabaseName("ix_journey_schedule_operation_date");
    }
}
