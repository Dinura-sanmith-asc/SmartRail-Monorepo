using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DriverOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "driver_journey_assignment",
                columns: table => new
                {
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    journey_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_journey_assignment", x => x.assignment_id);
                    table.UniqueConstraint("AK_driver_journey_assignment_assignment_id_driver_subject_jour~", x => new { x.assignment_id, x.driver_subject, x.journey_instance_id });
                    table.CheckConstraint("ck_driver_assignment_revocation", "(revoked_at_utc IS NULL AND revoked_by_subject IS NULL) OR (revoked_at_utc >= assigned_at_utc AND revoked_by_subject IS NOT NULL AND length(trim(revoked_by_subject)) > 0)");
                    table.CheckConstraint("ck_driver_assignment_text", "length(trim(driver_subject)) > 0 AND length(trim(assigned_by_subject)) > 0 AND length(trim(source_reference)) > 0");
                    table.ForeignKey(
                        name: "FK_driver_journey_assignment_journey_journey_instance_id",
                        column: x => x.journey_instance_id,
                        principalTable: "journey",
                        principalColumn: "journey_instance_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "driver_assignment_audit",
                columns: table => new
                {
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_assignment_audit", x => x.audit_id);
                    table.CheckConstraint("ck_driver_audit_action", "action IN ('Granted','Revoked')");
                    table.CheckConstraint("ck_driver_audit_text", "length(trim(actor_subject)) > 0 AND length(trim(evidence_reference)) > 0");
                    table.ForeignKey(
                        name: "FK_driver_assignment_audit_driver_journey_assignment_assignmen~",
                        column: x => x.assignment_id,
                        principalTable: "driver_journey_assignment",
                        principalColumn: "assignment_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "driver_observation",
                columns: table => new
                {
                    observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journey_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    reported_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    journey_revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_observation", x => x.observation_id);
                    table.CheckConstraint("ck_driver_observation_revision", "journey_revision > 0");
                    table.CheckConstraint("ck_driver_observation_text", "length(trim(text)) > 0 AND length(trim(driver_subject)) > 0");
                    table.ForeignKey(
                        name: "FK_driver_observation_driver_journey_assignment_assignment_id_~",
                        columns: x => new { x.assignment_id, x.driver_subject, x.journey_instance_id },
                        principalTable: "driver_journey_assignment",
                        principalColumns: new[] { "assignment_id", "driver_subject", "journey_instance_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_driver_observation_journey_revision_journey_instance_id_jou~",
                        columns: x => new { x.journey_instance_id, x.journey_revision },
                        principalTable: "journey_revision",
                        principalColumns: new[] { "journey_instance_id", "revision" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_driver_assignment_audit_assignment_id_occurred_at_utc",
                table: "driver_assignment_audit",
                columns: new[] { "assignment_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_driver_journey_assignment_driver_subject_journey_instance_id",
                table: "driver_journey_assignment",
                columns: new[] { "driver_subject", "journey_instance_id" },
                unique: true,
                filter: "revoked_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_driver_journey_assignment_journey_instance_id",
                table: "driver_journey_assignment",
                column: "journey_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_driver_observation_assignment_id_driver_subject_journey_ins~",
                table: "driver_observation",
                columns: new[] { "assignment_id", "driver_subject", "journey_instance_id" });

            migrationBuilder.CreateIndex(
                name: "IX_driver_observation_driver_subject_request_id",
                table: "driver_observation",
                columns: new[] { "driver_subject", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_driver_observation_journey_instance_id_journey_revision",
                table: "driver_observation",
                columns: new[] { "journey_instance_id", "journey_revision" });

            migrationBuilder.CreateIndex(
                name: "IX_driver_observation_journey_instance_id_reported_at_utc",
                table: "driver_observation",
                columns: new[] { "journey_instance_id", "reported_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "driver_assignment_audit");

            migrationBuilder.DropTable(
                name: "driver_observation");

            migrationBuilder.DropTable(
                name: "driver_journey_assignment");
        }
    }
}
