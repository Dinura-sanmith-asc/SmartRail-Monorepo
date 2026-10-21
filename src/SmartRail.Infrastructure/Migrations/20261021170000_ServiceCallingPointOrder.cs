using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartRail.Infrastructure.RailwayNetwork;

#nullable disable

namespace SmartRail.Infrastructure.Migrations;

[DbContext(typeof(SmartRailDbContext))]
[Migration("20261021170000_ServiceCallingPointOrder")]
public sealed class ServiceCallingPointOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "stop_order", table: "service_calling_point", type: "integer", nullable: true);
        migrationBuilder.Sql("UPDATE service_calling_point AS call SET stop_order = stop.stop_order FROM route_stop AS stop WHERE stop.route_stop_id = call.route_stop_id");
        migrationBuilder.AlterColumn<int>(name: "stop_order", table: "service_calling_point", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
        migrationBuilder.AddCheckConstraint(name: "ck_calling_point_order_positive", table: "service_calling_point", sql: "stop_order > 0");
        migrationBuilder.CreateIndex(name: "ux_calling_point_schedule_order", table: "service_calling_point", columns: new[] { "schedule_id", "stop_order" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ux_calling_point_schedule_order", table: "service_calling_point");
        migrationBuilder.DropCheckConstraint(name: "ck_calling_point_order_positive", table: "service_calling_point");
        migrationBuilder.DropColumn(name: "stop_order", table: "service_calling_point");
    }
}
