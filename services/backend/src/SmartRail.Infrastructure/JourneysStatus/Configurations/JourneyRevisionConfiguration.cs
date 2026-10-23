using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;

namespace SmartRail.Infrastructure.JourneysStatus.Configurations;

public sealed class JourneyRevisionConfiguration : IEntityTypeConfiguration<JourneyRevision>
{
    public void Configure(EntityTypeBuilder<JourneyRevision> builder)
    {
        builder.ToTable("journey_revision", table =>
        {
            table.HasCheckConstraint("ck_journey_revision_positive", "revision > 0");
            table.HasCheckConstraint("ck_journey_revision_previous", "previous_revision IS NULL OR (previous_revision > 0 AND previous_revision < revision)");
            table.HasCheckConstraint("ck_journey_revision_update_id", "(publication_kind = 'InitializationBaseline' AND update_id IS NULL) OR (publication_kind <> 'InitializationBaseline' AND update_id IS NOT NULL)");
            table.HasCheckConstraint("ck_journey_revision_source", "(publication_kind = 'InitializationBaseline' AND source = 'ScheduledBaseline') OR (publication_kind <> 'InitializationBaseline' AND source IN ('ControllerManual', 'StationStaffManual', 'ApprovedRailwayFeed'))");
            table.HasCheckConstraint("ck_journey_revision_actor", "publication_kind = 'InitializationBaseline' OR (published_by_subject IS NOT NULL AND length(trim(published_by_subject)) > 0)");
            table.HasCheckConstraint("ck_journey_revision_status", "status IN ('Scheduled','Running','Delayed','Interrupted','Cancelled','Completed','Terminated')");
        });
        builder.HasKey(x => new { x.JourneyInstanceId, x.Revision }).HasName("pk_journey_revision");
        builder.Property(x => x.JourneyInstanceId).HasColumnName("journey_instance_id");
        builder.Property(x => x.Revision).HasColumnName("revision");
        builder.Property(x => x.UpdateId).HasColumnName("update_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PublicationKind).HasColumnName("publication_kind").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.PreviousRevision).HasColumnName("previous_revision");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PublishedBySubject).HasColumnName("published_by_subject").HasMaxLength(300);
        builder.Property(x => x.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.DriverObservationId).HasColumnName("driver_observation_id");
        builder.Property(x => x.CorrectionOfUpdateId).HasColumnName("correction_of_update_id");
        builder.Property(x => x.PublicChangeType).HasColumnName("public_change_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PrivateAuditEvidence).HasColumnName("private_audit_evidence").HasMaxLength(4000).IsRequired();
        builder.HasOne<Journey>().WithMany().HasForeignKey(x => x.JourneyInstanceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_journey_revision_journey");
        builder.HasIndex(x => x.UpdateId).IsUnique().HasFilter("update_id IS NOT NULL").HasDatabaseName("ux_journey_revision_update_id");
        builder.HasIndex(x => new { x.JourneyInstanceId, x.Revision }).HasDatabaseName("ix_journey_revision_history");
        builder.HasIndex(x => new { x.JourneyInstanceId, x.PublishedAt }).HasDatabaseName("ix_journey_revision_published_at");
    }
}
