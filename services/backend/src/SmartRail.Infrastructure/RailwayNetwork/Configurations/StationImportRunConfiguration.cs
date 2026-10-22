using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class StationImportRunConfiguration : IEntityTypeConfiguration<StationImportRun>
{
    public void Configure(EntityTypeBuilder<StationImportRun> builder)
    {
        builder.ToTable("station_import_run", table =>
        {
            table.HasCheckConstraint("ck_station_import_run_counts_nonnegative", "created_count >= 0 AND updated_count >= 0 AND unchanged_count >= 0");
            table.HasCheckConstraint("ck_station_import_run_time_order", "completed_at >= started_at");
        });
        builder.HasKey(run => run.ImportRunId).HasName("pk_station_import_run");
        builder.Property(run => run.ImportRunId).HasColumnName("import_run_id").ValueGeneratedNever();
        builder.Property(run => run.SourceRevision).HasColumnName("source_revision").IsRequired();
        builder.Property(run => run.ContentHash).HasColumnName("content_hash").IsRequired();
        builder.Property(run => run.SubmittedBy).HasColumnName("submitted_by").IsRequired();
        builder.Property(run => run.ApprovedBy).HasColumnName("approved_by").IsRequired();
        builder.Property(run => run.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(run => run.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(run => run.CreatedCount).HasColumnName("created_count").IsRequired();
        builder.Property(run => run.UpdatedCount).HasColumnName("updated_count").IsRequired();
        builder.Property(run => run.UnchangedCount).HasColumnName("unchanged_count").IsRequired();
        builder.HasIndex(run => new { run.SourceRevision, run.StartedAt }).HasDatabaseName("ix_station_import_run_source_started");
    }
}