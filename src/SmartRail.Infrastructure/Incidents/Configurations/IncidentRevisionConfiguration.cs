using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.Infrastructure.Incidents.Configurations;

public sealed class IncidentRevisionConfiguration : IEntityTypeConfiguration<IncidentRevision>
{
    public void Configure(EntityTypeBuilder<IncidentRevision> builder)
    {
        // Prevent EF Core from generating UPDATE statements for immutable revision evidence;
        // SmartRailDbContext also rejects both Modified and Deleted entries at SaveChanges.
        foreach (var property in builder.Metadata.GetProperties())
            property.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
        builder.ToTable("incident_revision", table =>
        {
            table.HasCheckConstraint("ck_incident_revision_positive", "revision > 0");
            table.HasCheckConstraint("ck_incident_revision_state", "state IN ('Reported','InProgress','Resolved','Closed')");
            table.HasCheckConstraint("ck_incident_revision_transition", "previous_state IS NULL OR (previous_state='Reported' AND state='InProgress') OR (previous_state='InProgress' AND state='Resolved') OR (previous_state='Resolved' AND state='Closed')");
            table.HasCheckConstraint("ck_incident_revision_evidence", "length(trim(actor_subject)) > 0 AND length(trim(source)) > 0 AND length(association_evidence::text) > 0");
        });
        builder.HasKey(x => new { x.IncidentId, x.Revision }).HasName("pk_incident_revision");
        builder.Property(x => x.IncidentId).HasColumnName("incident_id");
        builder.Property(x => x.Revision).HasColumnName("revision");
        builder.Property(x => x.State).HasColumnName("state").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PreviousState).HasColumnName("previous_state").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasMaxLength(100).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.AssociationEvidence).HasColumnName("association_evidence").HasColumnType("jsonb").IsRequired();
        builder.HasOne<Incident>().WithMany(x => x.Revisions).HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_incident_revision_incident");
        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_incident_revision_occurred_at");
    }
}
