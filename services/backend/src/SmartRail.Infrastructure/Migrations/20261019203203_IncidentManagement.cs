using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncidentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incident",
                columns: table => new
                {
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    reporter_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    reported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incident", x => x.incident_id);
                    table.CheckConstraint("ck_incident_required_text", "length(trim(category_code)) > 0 AND length(trim(description)) > 0 AND length(trim(reporter_subject)) > 0");
                    table.CheckConstraint("ck_incident_revision_positive", "revision > 0");
                    table.CheckConstraint("ck_incident_state", "state IN ('Reported','InProgress','Resolved','Closed')");
                });

            migrationBuilder.CreateTable(
                name: "incident_association",
                columns: table => new
                {
                    association_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disruption_update_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disruption_revision = table.Column<int>(type: "integer", nullable: true),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    added_revision = table.Column<int>(type: "integer", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    removed_revision = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incident_association", x => x.association_id);
                    table.CheckConstraint("ck_incident_association_disruption", "(kind = 'Disruption' AND disruption_update_id IS NOT NULL AND disruption_revision > 0) OR (kind <> 'Disruption' AND disruption_update_id IS NULL AND disruption_revision IS NULL)");
                    table.CheckConstraint("ck_incident_association_kind", "kind IN ('Station','Service','Journey','Disruption')");
                    table.CheckConstraint("ck_incident_association_owner_reference", "(kind = 'Station' AND station_reference_id = resource_id AND service_reference_id IS NULL) OR (kind = 'Service' AND service_reference_id = resource_id AND station_reference_id IS NULL) OR (kind IN ('Journey','Disruption') AND station_reference_id IS NULL AND service_reference_id IS NULL)");
                    table.CheckConstraint("ck_incident_association_removal_pair", "(removed_at IS NULL AND removed_revision IS NULL) OR (removed_at IS NOT NULL AND removed_revision IS NOT NULL)");
                    table.CheckConstraint("ck_incident_association_resource", "resource_id <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_incident_association_revision", "added_revision > 0 AND (removed_revision IS NULL OR removed_revision > added_revision)");
                    table.ForeignKey(
                        name: "fk_incident_association_incident",
                        column: x => x.incident_id,
                        principalTable: "incident",
                        principalColumn: "incident_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incident_association_service",
                        column: x => x.service_reference_id,
                        principalTable: "train_service",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incident_association_station",
                        column: x => x.station_reference_id,
                        principalTable: "station",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incident_idempotency_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    http_status_code = table.Column<int>(type: "integer", nullable: false),
                    response_json = table.Column<string>(type: "jsonb", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incident_idempotency_record", x => x.id);
                    table.CheckConstraint("ck_incident_idempotency_required_text", "length(trim(actor_subject)) > 0 AND length(trim(operation)) > 0 AND length(trim(idempotency_key)) > 0 AND length(trim(payload_hash)) > 0 AND length(response_json::text) > 0");
                    table.CheckConstraint("ck_incident_idempotency_status", "http_status_code BETWEEN 200 AND 599");
                    table.ForeignKey(
                        name: "fk_incident_idempotency_incident",
                        column: x => x.incident_id,
                        principalTable: "incident",
                        principalColumn: "incident_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incident_revision",
                columns: table => new
                {
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    previous_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    actor_subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    association_evidence = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incident_revision", x => new { x.incident_id, x.revision });
                    table.CheckConstraint("ck_incident_revision_evidence", "length(trim(actor_subject)) > 0 AND length(trim(source)) > 0 AND length(association_evidence::text) > 0");
                    table.CheckConstraint("ck_incident_revision_positive", "revision > 0");
                    table.CheckConstraint("ck_incident_revision_state", "state IN ('Reported','InProgress','Resolved','Closed')");
                    table.CheckConstraint("ck_incident_revision_transition", "previous_state IS NULL OR (previous_state='Reported' AND state='InProgress') OR (previous_state='InProgress' AND state='Resolved') OR (previous_state='Resolved' AND state='Closed')");
                    table.ForeignKey(
                        name: "fk_incident_revision_incident",
                        column: x => x.incident_id,
                        principalTable: "incident",
                        principalColumn: "incident_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_incident_state_updated_at",
                table: "incident",
                columns: new[] { "state", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_incident_association_history",
                table: "incident_association",
                columns: new[] { "incident_id", "added_revision" });

            migrationBuilder.CreateIndex(
                name: "IX_incident_association_service_reference_id",
                table: "incident_association",
                column: "service_reference_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_association_station_reference_id",
                table: "incident_association",
                column: "station_reference_id");

            migrationBuilder.CreateIndex(
                name: "ux_incident_association_active_disruption",
                table: "incident_association",
                columns: new[] { "incident_id", "kind", "resource_id", "disruption_update_id", "disruption_revision" },
                unique: true,
                filter: "removed_revision IS NULL AND kind = 'Disruption'");

            migrationBuilder.CreateIndex(
                name: "ux_incident_association_active_resource",
                table: "incident_association",
                columns: new[] { "incident_id", "kind", "resource_id" },
                unique: true,
                filter: "removed_revision IS NULL AND kind <> 'Disruption'");

            migrationBuilder.CreateIndex(
                name: "IX_incident_idempotency_record_incident_id",
                table: "incident_idempotency_record",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ux_incident_idempotency_scope_key",
                table: "incident_idempotency_record",
                columns: new[] { "actor_subject", "operation", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_incident_revision_occurred_at",
                table: "incident_revision",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incident_association");

            migrationBuilder.DropTable(
                name: "incident_idempotency_record");

            migrationBuilder.DropTable(
                name: "incident_revision");

            migrationBuilder.DropTable(
                name: "incident");
        }
    }
}
