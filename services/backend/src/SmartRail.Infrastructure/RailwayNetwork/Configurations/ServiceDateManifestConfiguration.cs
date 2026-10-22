using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class ServiceDateManifestConfiguration : IEntityTypeConfiguration<ServiceDateManifest>
{
    public void Configure(EntityTypeBuilder<ServiceDateManifest> builder)
    {
        builder.ToTable("service_date_manifest", table =>
        {
            table.HasCheckConstraint("ck_service_date_manifest_publication_pair", "(published_at IS NULL AND published_by IS NULL) OR (published_at IS NOT NULL AND published_by IS NOT NULL)");
            table.HasCheckConstraint("ck_service_date_manifest_complete_evidence", "NOT is_complete OR (published_at IS NOT NULL AND published_by IS NOT NULL AND length(trim(content_hash)) > 0)");
        });
        builder.HasKey(manifest => manifest.OperationDate).HasName("pk_service_date_manifest");
        builder.Property(manifest => manifest.OperationDate).HasColumnName("operation_date").HasColumnType("date");
        builder.Property(manifest => manifest.SourceRevision).HasColumnName("source_revision").IsRequired();
        builder.Property(manifest => manifest.FixtureVersion).HasColumnName("fixture_version").IsRequired();
        builder.Property(manifest => manifest.IsComplete).HasColumnName("is_complete").IsRequired();
        builder.Property(manifest => manifest.PublishedAt).HasColumnName("published_at").HasColumnType("timestamp with time zone");
        builder.Property(manifest => manifest.PublishedBy).HasColumnName("published_by");
        builder.Property(manifest => manifest.ContentHash).HasColumnName("content_hash").IsRequired();
    }
}