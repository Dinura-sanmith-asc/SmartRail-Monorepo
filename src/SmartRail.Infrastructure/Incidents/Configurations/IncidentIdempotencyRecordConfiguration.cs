using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.Incidents.Entities;

namespace SmartRail.Infrastructure.Incidents.Configurations;

public sealed class IncidentIdempotencyRecordConfiguration : IEntityTypeConfiguration<IncidentIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IncidentIdempotencyRecord> builder)
    {
        builder.ToTable("incident_idempotency_record", table =>
        {
            table.HasCheckConstraint("ck_incident_idempotency_required_text", "length(trim(actor_subject)) > 0 AND length(trim(operation)) > 0 AND length(trim(idempotency_key)) > 0 AND length(trim(payload_hash)) > 0 AND length(response_json::text) > 0");
            table.HasCheckConstraint("ck_incident_idempotency_status", "http_status_code BETWEEN 200 AND 599");
        });
        builder.HasKey(x => x.Id).HasName("pk_incident_idempotency_record");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Operation).HasColumnName("operation").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.IncidentId).HasColumnName("incident_id").IsRequired();
        builder.Property(x => x.HttpStatusCode).HasColumnName("http_status_code").IsRequired();
        builder.Property(x => x.ResponseJson).HasColumnName("response_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne<Incident>().WithMany().HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_incident_idempotency_incident");
        builder.HasIndex(x => new { x.ActorSubject, x.Operation, x.IdempotencyKey }).IsUnique().HasDatabaseName("ux_incident_idempotency_scope_key");
    }
}
