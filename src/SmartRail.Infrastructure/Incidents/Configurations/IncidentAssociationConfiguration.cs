using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.Infrastructure.Incidents.Configurations;

public sealed class IncidentAssociationConfiguration : IEntityTypeConfiguration<IncidentAssociation>
{
    public void Configure(EntityTypeBuilder<IncidentAssociation> builder)
    {
        builder.ToTable("incident_association", table =>
        {
            table.HasCheckConstraint("ck_incident_association_kind", "kind IN ('Station','Service','Journey','Disruption')");
            table.HasCheckConstraint("ck_incident_association_resource", "resource_id <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint("ck_incident_association_owner_reference", "(kind = 'Station' AND station_reference_id = resource_id AND service_reference_id IS NULL) OR (kind = 'Service' AND service_reference_id = resource_id AND station_reference_id IS NULL) OR (kind IN ('Journey','Disruption') AND station_reference_id IS NULL AND service_reference_id IS NULL)");
            table.HasCheckConstraint("ck_incident_association_revision", "added_revision > 0 AND (removed_revision IS NULL OR removed_revision > added_revision)");
            table.HasCheckConstraint("ck_incident_association_removal_pair", "(removed_at IS NULL AND removed_revision IS NULL) OR (removed_at IS NOT NULL AND removed_revision IS NOT NULL)");
            table.HasCheckConstraint("ck_incident_association_disruption", "(kind = 'Disruption' AND disruption_update_id IS NOT NULL AND disruption_revision > 0) OR (kind <> 'Disruption' AND disruption_update_id IS NULL AND disruption_revision IS NULL)");
        });
        builder.HasKey(x => x.AssociationId).HasName("pk_incident_association");
        builder.Property(x => x.AssociationId).HasColumnName("association_id").ValueGeneratedNever();
        builder.Property(x => x.IncidentId).HasColumnName("incident_id").IsRequired();
        builder.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.StationReferenceId).HasColumnName("station_reference_id");
        builder.Property(x => x.ServiceReferenceId).HasColumnName("service_reference_id");
        builder.Property(x => x.DisruptionUpdateId).HasColumnName("disruption_update_id");
        builder.Property(x => x.DisruptionRevision).HasColumnName("disruption_revision");
        builder.Property(x => x.AddedAt).HasColumnName("added_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.AddedRevision).HasColumnName("added_revision").IsRequired();
        builder.Property(x => x.RemovedAt).HasColumnName("removed_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RemovedRevision).HasColumnName("removed_revision");
        builder.HasOne<Incident>().WithMany(x => x.Associations).HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_incident_association_incident");
        builder.HasOne<SmartRail.Domain.RailwayNetwork.Entities.Station>().WithMany().HasForeignKey(x => x.StationReferenceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_incident_association_station");
        builder.HasOne<SmartRail.Domain.RailwayNetwork.Entities.TrainService>().WithMany().HasForeignKey(x => x.ServiceReferenceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_incident_association_service");
        builder.HasIndex(x => new { x.IncidentId, x.AddedRevision }).HasDatabaseName("ix_incident_association_history");
        builder.HasIndex(x => new { x.IncidentId, x.Kind, x.ResourceId }).HasFilter("removed_revision IS NULL AND kind <> 'Disruption'").IsUnique().HasDatabaseName("ux_incident_association_active_resource");
        builder.HasIndex(x => new { x.IncidentId, x.Kind, x.ResourceId, x.DisruptionUpdateId, x.DisruptionRevision }).HasFilter("removed_revision IS NULL AND kind = 'Disruption'").IsUnique().HasDatabaseName("ux_incident_association_active_disruption");
    }
}
