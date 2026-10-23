using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultipleActiveServiceSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_schedule_current_published_service",
                table: "service_schedule_revision");

            migrationBuilder.AddColumn<Guid>(
                name: "slot_id",
                table: "service_schedule_revision",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Existing revisions were all versions of one timetable per service.
            migrationBuilder.Sql("""
                UPDATE service_schedule_revision AS schedule
                SET slot_id = (
                    SELECT first_revision.schedule_id
                    FROM service_schedule_revision AS first_revision
                    WHERE first_revision.service_id = schedule.service_id
                    ORDER BY first_revision.revision
                    LIMIT 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "ux_schedule_current_published_slot",
                table: "service_schedule_revision",
                columns: new[] { "service_id", "slot_id" },
                unique: true,
                filter: "is_published = TRUE AND superseded_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_schedule_current_published_slot",
                table: "service_schedule_revision");

            migrationBuilder.DropColumn(
                name: "slot_id",
                table: "service_schedule_revision");

            migrationBuilder.CreateIndex(
                name: "ux_schedule_current_published_service",
                table: "service_schedule_revision",
                column: "service_id",
                unique: true,
                filter: "is_published = TRUE AND superseded_at IS NULL");
        }
    }
}
