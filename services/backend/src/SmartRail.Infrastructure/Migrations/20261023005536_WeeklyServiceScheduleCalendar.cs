using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyServiceScheduleCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_schedule_calendar",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_through = table.Column<DateOnly>(type: "date", nullable: false),
                    weekdays_mask = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_schedule_calendar", x => x.schedule_id);
                    table.CheckConstraint("ck_schedule_calendar_dates", "valid_through >= valid_from");
                    table.ForeignKey(
                        name: "FK_service_schedule_calendar_service_schedule_revision_schedul~",
                        column: x => x.schedule_id,
                        principalTable: "service_schedule_revision",
                        principalColumn: "schedule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_schedule_date_exception",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    runs = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_schedule_date_exception", x => new { x.schedule_id, x.operation_date });
                    table.ForeignKey(
                        name: "FK_service_schedule_date_exception_service_schedule_revision_s~",
                        column: x => x.schedule_id,
                        principalTable: "service_schedule_revision",
                        principalColumn: "schedule_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_schedule_calendar");

            migrationBuilder.DropTable(
                name: "service_schedule_date_exception");
        }
    }
}
