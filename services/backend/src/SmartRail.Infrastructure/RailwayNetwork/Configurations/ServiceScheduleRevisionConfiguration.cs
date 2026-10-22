using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class ServiceScheduleRevisionConfiguration : IEntityTypeConfiguration<ServiceScheduleRevision>
{
    public void Configure(EntityTypeBuilder<ServiceScheduleRevision> builder)
    {
        builder.ToTable("service_schedule_revision", table => table.HasCheckConstraint("ck_schedule_revision_positive", "revision > 0"));
        builder.HasKey(schedule => schedule.ScheduleId).HasName("pk_service_schedule_revision");
        builder.Property(schedule => schedule.ScheduleId).HasColumnName("schedule_id").ValueGeneratedNever();
        builder.Property(schedule => schedule.ServiceId).HasColumnName("service_id").IsRequired();
        builder.Property(schedule => schedule.Revision).HasColumnName("revision").IsRequired();
        builder.Property(schedule => schedule.FirstDepartureLocalTime).HasColumnName("first_departure_local_time").HasColumnType("time without time zone").IsRequired();
        builder.Property(schedule => schedule.TimeZoneId).HasColumnName("time_zone_id").HasDefaultValue(ServiceScheduleRevision.RailwayTimeZoneId).IsRequired();
        builder.Property(schedule => schedule.IsPublished).HasColumnName("is_published").IsRequired();
        builder.Property(schedule => schedule.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(schedule => schedule.PublishedAt).HasColumnName("published_at").HasColumnType("timestamp with time zone");
        builder.Property(schedule => schedule.SupersededAt).HasColumnName("superseded_at").HasColumnType("timestamp with time zone");
        builder.HasOne<TrainService>().WithMany().HasForeignKey(schedule => schedule.ServiceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_schedule_revision_service");
        builder.HasIndex(schedule => new { schedule.ServiceId, schedule.Revision }).IsUnique().HasDatabaseName("ux_schedule_service_revision");
        builder.HasIndex(schedule => schedule.ServiceId).IsUnique()
            .HasFilter("is_published = TRUE AND superseded_at IS NULL")
            .HasDatabaseName("ux_schedule_current_published_service");
        builder.ToTable(table => table.HasCheckConstraint("ck_schedule_publication_timestamps", "(is_published AND published_at IS NOT NULL) OR (NOT is_published AND published_at IS NULL)"));
        builder.ToTable(table => table.HasCheckConstraint("ck_schedule_superseded_after_published", "superseded_at IS NULL OR (is_published AND superseded_at >= published_at)"));
    }
}