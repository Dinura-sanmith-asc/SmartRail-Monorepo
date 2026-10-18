using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.Identity.Entities;

namespace SmartRail.Infrastructure.Identity.Configurations;

public sealed class StationScopeAssignmentAuditConfiguration : IEntityTypeConfiguration<StationScopeAssignmentAudit>
{
    public void Configure(EntityTypeBuilder<StationScopeAssignmentAudit> builder)
    {
        builder.ToTable("identity_station_scope_assignment_audit", table =>
        {
            table.HasCheckConstraint("ck_identity_station_scope_audit_action", "action IN ('Granted', 'Revoked')");
            table.HasCheckConstraint("ck_identity_station_scope_audit_actor", "length(btrim(actor_subject)) > 0");
            table.HasCheckConstraint("ck_identity_station_scope_audit_evidence", "evidence_reference IS NULL OR length(btrim(evidence_reference)) > 0");
        });
        builder.HasKey(x => x.Id).HasName("pk_identity_station_scope_assignment_audit");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.StationScopeAssignmentId).HasColumnName("station_scope_assignment_id").IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").HasConversion<string>().HasColumnType("text").IsRequired();
        builder.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasColumnType("text").IsRequired();
        builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EvidenceReference).HasColumnName("evidence_reference").HasColumnType("text");
        builder.HasOne<StationScopeAssignment>().WithMany().HasForeignKey(x => x.StationScopeAssignmentId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_identity_station_scope_audit_assignment");
        builder.HasIndex(x => new { x.StationScopeAssignmentId, x.OccurredAtUtc })
            .HasDatabaseName("ix_identity_station_scope_audit_history");
    }
}
