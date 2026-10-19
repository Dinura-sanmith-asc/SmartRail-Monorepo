using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JourneysStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "journey",
                columns: table => new
                {
                    journey_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_revision = table.Column<int>(type: "integer", nullable: false),
                    operation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    first_departure_occurrence = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    source_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_instance_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    initialized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    initialized_by_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    initialization_provenance = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    current_revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journey", x => x.journey_instance_id);
                    table.CheckConstraint("ck_journey_current_revision_positive", "current_revision > 0");
                    table.CheckConstraint("ck_journey_required_text", "length(trim(source_system)) > 0 AND length(trim(source_instance_key)) > 0 AND length(trim(initialized_by_subject)) > 0 AND length(trim(initialization_provenance)) > 0");
                    table.CheckConstraint("ck_journey_schedule_revision_positive", "schedule_revision > 0");
                    table.ForeignKey(
                        name: "fk_journey_schedule",
                        column: x => x.schedule_id,
                        principalTable: "service_schedule_revision",
                        principalColumn: "schedule_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journey_service",
                        column: x => x.service_id,
                        principalTable: "train_service",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journey_initialization_audit",
                columns: table => new
                {
                    journey_initialization_audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_instance_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    journey_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    schedule_revision = table.Column<int>(type: "integer", nullable: true),
                    operation_date = table.Column<DateOnly>(type: "date", nullable: true),
                    first_departure_occurrence = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    provenance = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journey_initialization_audit", x => x.journey_initialization_audit_id);
                    table.CheckConstraint("ck_journey_init_audit_decision", "decision IN ('Accepted','IdempotentReplay','RejectedConflict','RejectedAuthorization','RejectedValidation')");
                    table.CheckConstraint("ck_journey_init_audit_revision", "schedule_revision IS NULL OR schedule_revision > 0");
                });

            migrationBuilder.CreateTable(
                name: "journey_revision",
                columns: table => new
                {
                    journey_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    update_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    publication_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    previous_revision = table.Column<int>(type: "integer", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_by_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    driver_observation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correction_of_update_id = table.Column<Guid>(type: "uuid", nullable: true),
                    public_change_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    private_audit_evidence = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journey_revision", x => new { x.journey_instance_id, x.revision });
                    table.CheckConstraint("ck_journey_revision_actor", "publication_kind = 'InitializationBaseline' OR (published_by_subject IS NOT NULL AND length(trim(published_by_subject)) > 0)");
                    table.CheckConstraint("ck_journey_revision_positive", "revision > 0");
                    table.CheckConstraint("ck_journey_revision_previous", "previous_revision IS NULL OR (previous_revision > 0 AND previous_revision < revision)");
                    table.CheckConstraint("ck_journey_revision_source", "(publication_kind = 'InitializationBaseline' AND source = 'ScheduledBaseline') OR (publication_kind <> 'InitializationBaseline' AND source IN ('ControllerManual', 'ApprovedRailwayFeed'))");
                    table.CheckConstraint("ck_journey_revision_status", "status IN ('Scheduled','Running','Delayed','Interrupted','Cancelled','Completed','Terminated')");
                    table.CheckConstraint("ck_journey_revision_update_id", "(publication_kind = 'InitializationBaseline' AND update_id IS NULL) OR (publication_kind <> 'InitializationBaseline' AND update_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_journey_revision_journey",
                        column: x => x.journey_instance_id,
                        principalTable: "journey",
                        principalColumn: "journey_instance_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journey_revision_stop",
                columns: table => new
                {
                    journey_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    journey_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_order = table.Column<int>(type: "integer", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_arrival_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    planned_departure_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    operational_arrival_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    operational_departure_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    timing_source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    timing_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ambiguity_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journey_revision_stop", x => new { x.journey_instance_id, x.revision, x.journey_stop_id });
                    table.CheckConstraint("ck_journey_stop_ambiguity", "ambiguity_state IN ('none','ambiguous')");
                    table.CheckConstraint("ck_journey_stop_order_positive", "stop_order > 0");
                    table.CheckConstraint("ck_journey_stop_platform_not_blank", "platform IS NULL OR length(trim(platform)) > 0");
                    table.ForeignKey(
                        name: "fk_journey_stop_revision",
                        columns: x => new { x.journey_instance_id, x.revision },
                        principalTable: "journey_revision",
                        principalColumns: new[] { "journey_instance_id", "revision" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journey_stop_route_stop",
                        column: x => x.route_stop_id,
                        principalTable: "route_stop",
                        principalColumn: "route_stop_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_journey_stop_station",
                        column: x => x.station_id,
                        principalTable: "station",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_journey_schedule_operation_date",
                table: "journey",
                columns: new[] { "schedule_id", "operation_date" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_service_operation_date",
                table: "journey",
                columns: new[] { "service_id", "operation_date" });

            migrationBuilder.CreateIndex(
                name: "ux_journey_source_key",
                table: "journey",
                columns: new[] { "source_system", "source_instance_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journey_init_audit_journey",
                table: "journey_initialization_audit",
                columns: new[] { "journey_instance_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_init_audit_source",
                table: "journey_initialization_audit",
                columns: new[] { "source_system", "source_instance_key", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_revision_history",
                table: "journey_revision",
                columns: new[] { "journey_instance_id", "revision" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_revision_published_at",
                table: "journey_revision",
                columns: new[] { "journey_instance_id", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ux_journey_revision_update_id",
                table: "journey_revision",
                column: "update_id",
                unique: true,
                filter: "update_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_journey_revision_stop_route_stop_id",
                table: "journey_revision_stop",
                column: "route_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_journey_stop_station",
                table: "journey_revision_stop",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ux_journey_stop_revision_order",
                table: "journey_revision_stop",
                columns: new[] { "journey_instance_id", "revision", "stop_order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "journey_initialization_audit");

            migrationBuilder.DropTable(
                name: "journey_revision_stop");

            migrationBuilder.DropTable(
                name: "journey_revision");

            migrationBuilder.DropTable(
                name: "journey");
        }
    }
}
