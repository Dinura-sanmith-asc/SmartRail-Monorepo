using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartRail.Infrastructure.RailwayNetwork;

#nullable disable

namespace SmartRail.Infrastructure.Migrations;

[DbContext(typeof(SmartRailDbContext))]
[Migration("20261021150000_UniquePublicServiceNumber")]
public sealed class UniquePublicServiceNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateIndex(
            name: "ux_train_service_public_service_number",
            table: "train_service",
            column: "public_service_number",
            unique: true,
            filter: "public_service_number IS NOT NULL");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropIndex(name: "ux_train_service_public_service_number", table: "train_service");
}
