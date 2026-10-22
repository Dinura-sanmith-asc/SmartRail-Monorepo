using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartRail.Infrastructure.RailwayNetwork;

#nullable disable

namespace SmartRail.Infrastructure.Migrations;

[DbContext(typeof(SmartRailDbContext))]
[Migration("20261023120000_TrainServiceRouteStops")]
public sealed class TrainServiceRouteStops : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "train_service_route_stop",
            columns: table => new
            {
                service_id = table.Column<Guid>(type: "uuid", nullable: false),
                route_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                stop_order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_train_service_route_stop", x => new { x.service_id, x.route_stop_id });
                table.CheckConstraint("ck_train_service_route_stop_order_positive", "stop_order > 0");
                table.ForeignKey(name: "fk_train_service_route_stop_route_stop", column: x => x.route_stop_id,
                    principalTable: "route_stop", principalColumn: "route_stop_id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "fk_train_service_route_stop_service", column: x => x.service_id,
                    principalTable: "train_service", principalColumn: "service_id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "ux_train_service_route_stop_order", table: "train_service_route_stop",
            columns: new[] { "service_id", "stop_order" }, unique: true);
        migrationBuilder.CreateIndex(name: "ix_train_service_route_stop_route_stop_id", table: "train_service_route_stop", column: "route_stop_id");
        migrationBuilder.Sql("""
            INSERT INTO train_service_route_stop (service_id, route_stop_id, stop_order)
            SELECT service.service_id, stop.route_stop_id,
                   ROW_NUMBER() OVER (PARTITION BY service.service_id ORDER BY stop.stop_order)::integer
            FROM train_service AS service
            JOIN route_stop AS stop ON stop.route_id = service.route_id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "train_service_route_stop");
}
