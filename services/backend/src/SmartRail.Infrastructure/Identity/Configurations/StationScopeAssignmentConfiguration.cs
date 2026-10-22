using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.Identity.Configurations;

public sealed class StationScopeAssignmentConfiguration : IEntityTypeConfiguration<StationScopeAssignment>
{
    public void Configure(EntityTypeBuilder<StationScopeAssignment> builder)
    {
        builder.ToTable("identity_station_scope_assignment", table =>
        {
            table.HasCheckConstraint("ck_identity_station_scope_subject", "length(btrim(cognito_subject)) > 0");
            table.HasCheckConstraint("ck_identity_station_scope_granted_by", "length(btrim(granted_by_subject)) > 0");
            table.HasCheckConstraint("ck_identity_station_scope_revocation_pair", "(revoked_at_utc IS NULL AND revoked_by_subject IS NULL) OR (revoked_at_utc IS NOT NULL AND length(btrim(revoked_by_subject)) > 0)");
            table.HasCheckConstraint("ck_identity_station_scope_revocation_after_grant", "revoked_at_utc IS NULL OR revoked_at_utc >= granted_at_utc");
        });
        builder.HasKey(x => x.Id).HasName("pk_identity_station_scope_assignment");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CognitoSubject).HasColumnName("cognito_subject").HasColumnType("text").IsRequired();
        builder.Property(x => x.StationId).HasColumnName("station_id").IsRequired();
        builder.Property(x => x.GrantedAtUtc).HasColumnName("granted_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.GrantedBySubject).HasColumnName("granted_by_subject").HasColumnType("text").IsRequired();
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedBySubject).HasColumnName("revoked_by_subject").HasColumnType("text");
        builder.HasOne<Station>().WithMany().HasForeignKey(x => x.StationId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_identity_station_scope_station");
        builder.HasIndex(x => new { x.CognitoSubject, x.StationId })
            .IsUnique().HasFilter("revoked_at_utc IS NULL AND revoked_by_subject IS NULL")
            .HasDatabaseName("ux_identity_station_scope_active_subject_station");
        builder.HasIndex(x => x.CognitoSubject).HasFilter("revoked_at_utc IS NULL AND revoked_by_subject IS NULL")
            .HasDatabaseName("ix_identity_station_scope_active_subject");
    }
}
