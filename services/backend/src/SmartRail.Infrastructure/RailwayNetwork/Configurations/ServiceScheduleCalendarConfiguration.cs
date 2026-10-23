using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Infrastructure.RailwayNetwork.Configurations;

public sealed class ServiceScheduleCalendarConfiguration : IEntityTypeConfiguration<ServiceScheduleCalendar>
{
    public void Configure(EntityTypeBuilder<ServiceScheduleCalendar> builder)
    {
        builder.ToTable("service_schedule_calendar", table => table.HasCheckConstraint("ck_schedule_calendar_dates", "valid_through >= valid_from"));
        builder.HasKey(item => item.ScheduleId);
        builder.Property(item => item.ScheduleId).HasColumnName("schedule_id");
        builder.Property(item => item.ValidFrom).HasColumnName("valid_from");
        builder.Property(item => item.ValidThrough).HasColumnName("valid_through");
        builder.Property(item => item.WeekdaysMask).HasColumnName("weekdays_mask");
        builder.HasOne<ServiceScheduleRevision>().WithOne().HasForeignKey<ServiceScheduleCalendar>(item => item.ScheduleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ServiceScheduleDateExceptionConfiguration : IEntityTypeConfiguration<ServiceScheduleDateException>
{
    public void Configure(EntityTypeBuilder<ServiceScheduleDateException> builder)
    {
        builder.ToTable("service_schedule_date_exception");
        builder.HasKey(item => new { item.ScheduleId, item.Date });
        builder.Property(item => item.ScheduleId).HasColumnName("schedule_id");
        builder.Property(item => item.Date).HasColumnName("operation_date");
        builder.Property(item => item.Runs).HasColumnName("runs");
        builder.HasOne<ServiceScheduleRevision>().WithMany().HasForeignKey(item => item.ScheduleId).OnDelete(DeleteBehavior.Cascade);
    }
}
