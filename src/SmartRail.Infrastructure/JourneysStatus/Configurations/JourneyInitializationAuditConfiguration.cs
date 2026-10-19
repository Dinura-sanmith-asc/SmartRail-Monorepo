using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.JourneysStatus.Entities;

namespace SmartRail.Infrastructure.JourneysStatus.Configurations;

public sealed class JourneyInitializationAuditConfiguration : IEntityTypeConfiguration<JourneyInitializationAudit>
{
    public void Configure(EntityTypeBuilder<JourneyInitializationAudit> builder)
    {
        builder.ToTable("journey_initialization_audit", table =>
        {
            table.HasCheckConstraint("ck_journey_init_audit_decision", "decision IN ('Accepted','IdempotentReplay','RejectedConflict','RejectedAuthorization','RejectedValidation')");
            table.HasCheckConstraint("ck_journey_init_audit_revision", "schedule_revision IS NULL OR schedule_revision > 0");
        });
        builder.HasKey(x => x.JourneyInitializationAuditId).HasName("pk_journey_initialization_audit");
        builder.Property(x => x.JourneyInitializationAuditId).HasColumnName("journey_initialization_audit_id").ValueGeneratedNever();
        builder.Property(x => x.SourceSystem).HasColumnName("source_system").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceInstanceKey).HasColumnName("source_instance_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.JourneyInstanceId).HasColumnName("journey_instance_id");
        builder.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(300);
        builder.Property(x => x.Decision).HasColumnName("decision").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ServiceId).HasColumnName("service_id");
        builder.Property(x => x.ScheduleId).HasColumnName("schedule_id");
        builder.Property(x => x.ScheduleRevision).HasColumnName("schedule_revision");
        builder.Property(x => x.OperationDate).HasColumnName("operation_date").HasColumnType("date");
        builder.Property(x => x.FirstDepartureOccurrence).HasColumnName("first_departure_occurrence").HasColumnType("time without time zone");
        builder.Property(x => x.Provenance).HasColumnName("provenance").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => new { x.SourceSystem, x.SourceInstanceKey, x.OccurredAt }).HasDatabaseName("ix_journey_init_audit_source");
        builder.HasIndex(x => new { x.JourneyInstanceId, x.OccurredAt }).HasDatabaseName("ix_journey_init_audit_journey");
    }
}
