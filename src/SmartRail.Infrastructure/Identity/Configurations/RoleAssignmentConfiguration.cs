using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;

namespace SmartRail.Infrastructure.Identity.Configurations;

public sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("identity_role_assignment", table =>
        {
            table.HasCheckConstraint("ck_identity_role_assignment_subject", "length(btrim(cognito_subject)) > 0");
            table.HasCheckConstraint("ck_identity_role_assignment_granted_by", "length(btrim(granted_by_subject)) > 0");
            table.HasCheckConstraint("ck_identity_role_assignment_role", "role_code IN ('Passenger', 'Driver', 'StationStaff', 'Controller')");
            table.HasCheckConstraint("ck_identity_role_assignment_revocation_pair", "(revoked_at_utc IS NULL AND revoked_by_subject IS NULL) OR (revoked_at_utc IS NOT NULL AND length(btrim(revoked_by_subject)) > 0)");
            table.HasCheckConstraint("ck_identity_role_assignment_revocation_after_grant", "revoked_at_utc IS NULL OR revoked_at_utc >= granted_at_utc");
        });
        builder.HasKey(x => x.Id).HasName("pk_identity_role_assignment");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CognitoSubject).HasColumnName("cognito_subject").HasColumnType("text").IsRequired();
        builder.Property(x => x.RoleCode).HasColumnName("role_code").HasConversion<string>().HasColumnType("text").IsRequired();
        builder.Property(x => x.GrantedAtUtc).HasColumnName("granted_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.GrantedBySubject).HasColumnName("granted_by_subject").HasColumnType("text").IsRequired();
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedBySubject).HasColumnName("revoked_by_subject").HasColumnType("text");
        builder.HasIndex(x => new { x.CognitoSubject, x.RoleCode })
            .IsUnique().HasFilter("revoked_at_utc IS NULL AND revoked_by_subject IS NULL")
            .HasDatabaseName("ux_identity_role_assignment_active_subject_role");
        builder.HasIndex(x => x.CognitoSubject).HasFilter("revoked_at_utc IS NULL AND revoked_by_subject IS NULL")
            .HasDatabaseName("ix_identity_role_assignment_active_subject");
    }
}
