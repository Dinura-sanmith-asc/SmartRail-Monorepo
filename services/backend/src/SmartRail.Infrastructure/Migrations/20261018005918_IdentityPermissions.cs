using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdentityPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "identity_role_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cognito_subject = table.Column<string>(type: "text", nullable: false),
                    role_code = table.Column<string>(type: "text", nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by_subject = table.Column<string>(type: "text", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_subject = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_role_assignment", x => x.id);
                    table.CheckConstraint("ck_identity_role_assignment_granted_by", "length(btrim(granted_by_subject)) > 0");
                    table.CheckConstraint("ck_identity_role_assignment_revocation_after_grant", "revoked_at_utc IS NULL OR revoked_at_utc >= granted_at_utc");
                    table.CheckConstraint("ck_identity_role_assignment_revocation_pair", "(revoked_at_utc IS NULL AND revoked_by_subject IS NULL) OR (revoked_at_utc IS NOT NULL AND length(btrim(revoked_by_subject)) > 0)");
                    table.CheckConstraint("ck_identity_role_assignment_role", "role_code IN ('Passenger', 'Driver', 'StationStaff', 'Controller')");
                    table.CheckConstraint("ck_identity_role_assignment_subject", "length(btrim(cognito_subject)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "identity_station_scope_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cognito_subject = table.Column<string>(type: "text", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by_subject = table.Column<string>(type: "text", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_subject = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_station_scope_assignment", x => x.id);
                    table.CheckConstraint("ck_identity_station_scope_granted_by", "length(btrim(granted_by_subject)) > 0");
                    table.CheckConstraint("ck_identity_station_scope_revocation_after_grant", "revoked_at_utc IS NULL OR revoked_at_utc >= granted_at_utc");
                    table.CheckConstraint("ck_identity_station_scope_revocation_pair", "(revoked_at_utc IS NULL AND revoked_by_subject IS NULL) OR (revoked_at_utc IS NOT NULL AND length(btrim(revoked_by_subject)) > 0)");
                    table.CheckConstraint("ck_identity_station_scope_subject", "length(btrim(cognito_subject)) > 0");
                    table.ForeignKey(
                        name: "fk_identity_station_scope_station",
                        column: x => x.station_id,
                        principalTable: "station",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "identity_role_assignment_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    actor_subject = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evidence_reference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_role_assignment_audit", x => x.id);
                    table.CheckConstraint("ck_identity_role_assignment_audit_action", "action IN ('Granted', 'Revoked')");
                    table.CheckConstraint("ck_identity_role_assignment_audit_actor", "length(btrim(actor_subject)) > 0");
                    table.CheckConstraint("ck_identity_role_assignment_audit_evidence", "evidence_reference IS NULL OR length(btrim(evidence_reference)) > 0");
                    table.ForeignKey(
                        name: "fk_identity_role_assignment_audit_assignment",
                        column: x => x.role_assignment_id,
                        principalTable: "identity_role_assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "identity_station_scope_assignment_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_scope_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    actor_subject = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evidence_reference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_station_scope_assignment_audit", x => x.id);
                    table.CheckConstraint("ck_identity_station_scope_audit_action", "action IN ('Granted', 'Revoked')");
                    table.CheckConstraint("ck_identity_station_scope_audit_actor", "length(btrim(actor_subject)) > 0");
                    table.CheckConstraint("ck_identity_station_scope_audit_evidence", "evidence_reference IS NULL OR length(btrim(evidence_reference)) > 0");
                    table.ForeignKey(
                        name: "fk_identity_station_scope_audit_assignment",
                        column: x => x.station_scope_assignment_id,
                        principalTable: "identity_station_scope_assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_identity_role_assignment_active_subject",
                table: "identity_role_assignment",
                column: "cognito_subject",
                filter: "revoked_at_utc IS NULL AND revoked_by_subject IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_identity_role_assignment_active_subject_role",
                table: "identity_role_assignment",
                columns: new[] { "cognito_subject", "role_code" },
                unique: true,
                filter: "revoked_at_utc IS NULL AND revoked_by_subject IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_role_assignment_audit_history",
                table: "identity_role_assignment_audit",
                columns: new[] { "role_assignment_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_station_scope_active_subject",
                table: "identity_station_scope_assignment",
                column: "cognito_subject",
                filter: "revoked_at_utc IS NULL AND revoked_by_subject IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_identity_station_scope_assignment_station_id",
                table: "identity_station_scope_assignment",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ux_identity_station_scope_active_subject_station",
                table: "identity_station_scope_assignment",
                columns: new[] { "cognito_subject", "station_id" },
                unique: true,
                filter: "revoked_at_utc IS NULL AND revoked_by_subject IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_station_scope_audit_history",
                table: "identity_station_scope_assignment_audit",
                columns: new[] { "station_scope_assignment_id", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_role_assignment_audit");

            migrationBuilder.DropTable(
                name: "identity_station_scope_assignment_audit");

            migrationBuilder.DropTable(
                name: "identity_role_assignment");

            migrationBuilder.DropTable(
                name: "identity_station_scope_assignment");
        }
    }
}
