using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.DriverOperations.Entities;
using SmartRail.Domain.JourneysStatus.Entities;

namespace SmartRail.Infrastructure.DriverOperations;

public sealed class DriverJourneyAssignmentConfiguration : IEntityTypeConfiguration<DriverJourneyAssignment>
{
    public void Configure(EntityTypeBuilder<DriverJourneyAssignment> b)
    {
        b.ToTable("driver_journey_assignment", t => {
            t.HasCheckConstraint("ck_driver_assignment_text", "length(trim(driver_subject)) > 0 AND length(trim(assigned_by_subject)) > 0 AND length(trim(source_reference)) > 0");
            t.HasCheckConstraint("ck_driver_assignment_revocation", "(revoked_at_utc IS NULL AND revoked_by_subject IS NULL) OR (revoked_at_utc >= assigned_at_utc AND revoked_by_subject IS NOT NULL AND length(trim(revoked_by_subject)) > 0)");
        });
        b.HasKey(x => x.AssignmentId);
        b.Property(x => x.AssignmentId).HasColumnName("assignment_id").ValueGeneratedNever();
        b.Property(x => x.DriverSubject).HasColumnName("driver_subject").HasMaxLength(300).IsRequired();
        b.Property(x => x.JourneyInstanceId).HasColumnName("journey_instance_id");
        b.Property(x => x.AssignedAtUtc).HasColumnName("assigned_at_utc");
        b.Property(x => x.AssignedBySubject).HasColumnName("assigned_by_subject").HasMaxLength(300).IsRequired();
        b.Property(x => x.SourceReference).HasColumnName("source_reference").HasMaxLength(1000).IsRequired();
        b.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").IsConcurrencyToken();
        b.Property(x => x.RevokedBySubject).HasColumnName("revoked_by_subject").HasMaxLength(300);
        b.Ignore(x => x.IsActive);
        b.HasOne<Journey>().WithMany().HasForeignKey(x => x.JourneyInstanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.AssignmentId, x.DriverSubject, x.JourneyInstanceId });
        b.HasIndex(x => new { x.DriverSubject, x.JourneyInstanceId }).IsUnique().HasFilter("revoked_at_utc IS NULL");
    }
}

public sealed class DriverAssignmentAuditConfiguration : IEntityTypeConfiguration<DriverAssignmentAudit>
{
    public void Configure(EntityTypeBuilder<DriverAssignmentAudit> b)
    {
        b.ToTable("driver_assignment_audit", t => {
            t.HasCheckConstraint("ck_driver_audit_action", "action IN ('Granted','Revoked')");
            t.HasCheckConstraint("ck_driver_audit_text", "length(trim(actor_subject)) > 0 AND length(trim(evidence_reference)) > 0");
        });
        b.HasKey(x => x.AuditId);
        b.Property(x => x.AuditId).HasColumnName("audit_id").ValueGeneratedNever();
        b.Property(x => x.AssignmentId).HasColumnName("assignment_id");
        b.Property(x => x.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(300).IsRequired();
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.Property(x => x.EvidenceReference).HasColumnName("evidence_reference").HasMaxLength(1000).IsRequired();
        b.HasOne<DriverJourneyAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.AssignmentId, x.OccurredAtUtc });
    }
}

public sealed class DriverObservationConfiguration : IEntityTypeConfiguration<DriverObservation>
{
    public void Configure(EntityTypeBuilder<DriverObservation> b)
    {
        b.ToTable("driver_observation", t => {
            t.HasCheckConstraint("ck_driver_observation_text", "length(trim(text)) > 0 AND length(trim(driver_subject)) > 0");
            t.HasCheckConstraint("ck_driver_observation_revision", "journey_revision > 0");
        });
        b.HasKey(x => x.ObservationId);
        b.Property(x => x.ObservationId).HasColumnName("observation_id").ValueGeneratedNever();
        b.Property(x => x.AssignmentId).HasColumnName("assignment_id");
        b.Property(x => x.JourneyInstanceId).HasColumnName("journey_instance_id");
        b.Property(x => x.DriverSubject).HasColumnName("driver_subject").HasMaxLength(300).IsRequired();
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.Text).HasColumnName("text").HasMaxLength(4000).IsRequired();
        b.Property(x => x.ReportedAtUtc).HasColumnName("reported_at_utc");
        b.Property(x => x.JourneyRevision).HasColumnName("journey_revision");
        b.HasOne<DriverJourneyAssignment>().WithMany().HasForeignKey(x => new { x.AssignmentId, x.DriverSubject, x.JourneyInstanceId })
            .HasPrincipalKey(x => new { x.AssignmentId, x.DriverSubject, x.JourneyInstanceId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JourneyRevision>().WithMany().HasForeignKey(x => new { x.JourneyInstanceId, x.JourneyRevision }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.DriverSubject, x.RequestId }).IsUnique();
        b.HasIndex(x => new { x.JourneyInstanceId, x.ReportedAtUtc });
    }
}
