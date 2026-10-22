using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class StationChangeConfiguration : IEntityTypeConfiguration<StationChange>
{
    public void Configure(EntityTypeBuilder<StationChange> builder)
    {
        builder.ToTable("station_change", table =>
        {
            table.HasCheckConstraint("ck_station_change_action", "action IN ('Created', 'Updated')");
            table.HasCheckConstraint("ck_station_change_fields_nonempty", "cardinality(changed_field_names) > 0 AND array_position(changed_field_names, '') IS NULL");
        });
        builder.HasKey(change => change.StationChangeId).HasName("pk_station_change");
        builder.Property(change => change.StationChangeId).HasColumnName("station_change_id").ValueGeneratedNever();
        builder.Property(change => change.ImportRunId).HasColumnName("import_run_id").IsRequired();
        builder.Property(change => change.StationId).HasColumnName("station_id").IsRequired();
        builder.Property(change => change.Action).HasColumnName("action").HasConversion<string>().HasColumnType("text").IsRequired();
        var fieldsConverter = new ValueConverter<IReadOnlyList<string>, string[]>(
            fields => fields.ToArray(),
            fields => fields);
        var fieldsComparer = new ValueComparer<IReadOnlyList<string>>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, field) => HashCode.Combine(hash, field)),
            value => (IReadOnlyList<string>)value.ToArray());
        var fieldsProperty = builder.Property(change => change.ChangedFieldNames)
            .HasColumnName("changed_field_names")
            .HasColumnType("text[]")
            .HasConversion(fieldsConverter)
            .IsRequired();
        fieldsProperty.Metadata.SetValueComparer(fieldsComparer);
        builder.Property(change => change.ChangedAt).HasColumnName("changed_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(change => change.ChangedBy).HasColumnName("changed_by").IsRequired();
        builder.HasOne<StationImportRun>().WithMany().HasForeignKey(change => change.ImportRunId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_station_change_import_run");
        builder.HasOne<Station>().WithMany().HasForeignKey(change => change.StationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_station_change_station");
        builder.HasIndex(change => change.ImportRunId).HasDatabaseName("ix_station_change_import_run");
        builder.HasIndex(change => new { change.StationId, change.ChangedAt }).HasDatabaseName("ix_station_change_station_changed_at");
    }
}
