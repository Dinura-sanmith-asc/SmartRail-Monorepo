using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.Infrastructure.Incidents.Configurations;

public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incident", table =>
        {
            table.HasCheckConstraint("ck_incident_revision_positive", "revision > 0");
            table.HasCheckConstraint("ck_incident_required_text", "length(trim(category_code)) > 0 AND length(trim(description)) > 0 AND length(trim(reporter_subject)) > 0");
            table.HasCheckConstraint("ck_incident_state", "state IN ('Reported','InProgress','Resolved','Closed')");
        });
        builder.HasKey(x => x.IncidentId).HasName("pk_incident");
        builder.Property(x => x.IncidentId).HasColumnName("incident_id").ValueGeneratedNever();
        builder.Property(x => x.CategoryCode).HasColumnName("category_code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();
        builder.Property(x => x.State).HasColumnName("state").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Revision).HasColumnName("revision").IsConcurrencyToken().IsRequired();
        builder.Property(x => x.ReporterSubject).HasColumnName("reporter_subject").HasMaxLength(300).IsRequired();
        builder.Property(x => x.ReportedAt).HasColumnName("reported_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasMany(x => x.Revisions).WithOne().HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_incident_revision_incident");
        builder.HasMany(x => x.Associations).WithOne().HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_incident_association_incident");
        builder.HasIndex(x => new { x.State, x.UpdatedAt }).HasDatabaseName("ix_incident_state_updated_at");
    }
}
