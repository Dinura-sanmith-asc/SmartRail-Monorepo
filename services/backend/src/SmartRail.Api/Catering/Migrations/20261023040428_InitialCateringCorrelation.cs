using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Api.Catering.Migrations
{
    /// <inheritdoc />
    public partial class InitialCateringCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catering_order_correlations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PassengerSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JourneyInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catering_order_correlations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catering_order_correlations_PassengerSubject_IdempotencyKey",
                table: "catering_order_correlations",
                columns: new[] { "PassengerSubject", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catering_order_correlations_ProviderOrderId",
                table: "catering_order_correlations",
                column: "ProviderOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catering_order_correlations");
        }
    }
}
