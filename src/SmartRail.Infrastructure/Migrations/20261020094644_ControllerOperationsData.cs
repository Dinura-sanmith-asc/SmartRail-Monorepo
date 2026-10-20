using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ControllerOperationsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "train_service",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_service_number",
                table: "train_service",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_name",
                table: "train_service");

            migrationBuilder.DropColumn(
                name: "public_service_number",
                table: "train_service");
        }
    }
}
