using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialRailwayNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "route",
                columns: table => new
                {
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_route", x => x.route_id);
                    table.CheckConstraint("ck_route_version_positive", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "service_date_manifest",
                columns: table => new
                {
                    operation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source_revision = table.Column<string>(type: "text", nullable: false),
                    fixture_version = table.Column<string>(type: "text", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<string>(type: "text", nullable: true),
                    content_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_date_manifest", x => x.operation_date);
                    table.CheckConstraint("ck_service_date_manifest_complete_evidence", "NOT is_complete OR (published_at IS NOT NULL AND published_by IS NOT NULL AND length(trim(content_hash)) > 0)");
                    table.CheckConstraint("ck_service_date_manifest_publication_pair", "(published_at IS NULL AND published_by IS NULL) OR (published_at IS NOT NULL AND published_by IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "station",
                columns: table => new
                {
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    verified_details = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    source_station_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_station", x => x.station_id);
                    table.CheckConstraint("ck_station_name_not_blank", "length(trim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "station_import_run",
                columns: table => new
                {
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_revision = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "text", nullable: false),
                    submitted_by = table.Column<string>(type: "text", nullable: false),
                    approved_by = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    unchanged_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_station_import_run", x => x.import_run_id);
                    table.CheckConstraint("ck_station_import_run_counts_nonnegative", "created_count >= 0 AND updated_count >= 0 AND unchanged_count >= 0");
                    table.CheckConstraint("ck_station_import_run_time_order", "completed_at >= started_at");
                });

            migrationBuilder.CreateTable(
                name: "train_service",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_code = table.Column<string>(type: "text", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_train_service", x => x.service_id);
                    table.CheckConstraint("ck_train_service_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_train_service_route",
                        column: x => x.route_id,
                        principalTable: "route",
                        principalColumn: "route_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "route_stop",
                columns: table => new
                {
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_route_stop", x => x.route_stop_id);
                    table.CheckConstraint("ck_route_stop_order_positive", "stop_order > 0");
                    table.ForeignKey(
                        name: "fk_route_stop_route",
                        column: x => x.route_id,
                        principalTable: "route",
                        principalColumn: "route_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_route_stop_station",
                        column: x => x.station_id,
                        principalTable: "station",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "station_change",
                columns: table => new
                {
                    station_change_id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    changed_field_names = table.Column<string[]>(type: "text[]", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_station_change", x => x.station_change_id);
                    table.CheckConstraint("ck_station_change_action", "action IN ('Created', 'Updated')");
                    table.CheckConstraint("ck_station_change_fields_nonempty", "cardinality(changed_field_names) > 0 AND array_position(changed_field_names, '') IS NULL");
                    table.ForeignKey(
                        name: "fk_station_change_import_run",
                        column: x => x.import_run_id,
                        principalTable: "station_import_run",
                        principalColumn: "import_run_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_station_change_station",
                        column: x => x.station_id,
                        principalTable: "station",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_schedule_revision",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    first_departure_local_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_zone_id = table.Column<string>(type: "text", nullable: false, defaultValue: "Asia/Colombo"),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    superseded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_schedule_revision", x => x.schedule_id);
                    table.CheckConstraint("ck_schedule_publication_timestamps", "(is_published AND published_at IS NOT NULL) OR (NOT is_published AND published_at IS NULL)");
                    table.CheckConstraint("ck_schedule_revision_positive", "revision > 0");
                    table.CheckConstraint("ck_schedule_superseded_after_published", "superseded_at IS NULL OR (is_published AND superseded_at >= published_at)");
                    table.ForeignKey(
                        name: "fk_schedule_revision_service",
                        column: x => x.service_id,
                        principalTable: "train_service",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_calling_point",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    arrival_offset_minutes = table.Column<int>(type: "integer", nullable: true),
                    departure_offset_minutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_calling_point", x => new { x.schedule_id, x.route_stop_id });
                    table.CheckConstraint("ck_calling_point_arrival_before_departure", "arrival_offset_minutes IS NULL OR departure_offset_minutes IS NULL OR arrival_offset_minutes <= departure_offset_minutes");
                    table.CheckConstraint("ck_calling_point_arrival_nonnegative", "arrival_offset_minutes IS NULL OR arrival_offset_minutes >= 0");
                    table.CheckConstraint("ck_calling_point_departure_nonnegative", "departure_offset_minutes IS NULL OR departure_offset_minutes >= 0");
                    table.ForeignKey(
                        name: "fk_calling_point_route_stop",
                        column: x => x.route_stop_id,
                        principalTable: "route_stop",
                        principalColumn: "route_stop_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_calling_point_schedule",
                        column: x => x.schedule_id,
                        principalTable: "service_schedule_revision",
                        principalColumn: "schedule_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_operation_date",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_operation_date", x => new { x.schedule_id, x.operation_date });
                    table.ForeignKey(
                        name: "fk_service_operation_manifest",
                        column: x => x.operation_date,
                        principalTable: "service_date_manifest",
                        principalColumn: "operation_date",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_service_operation_schedule",
                        column: x => x.schedule_id,
                        principalTable: "service_schedule_revision",
                        principalColumn: "schedule_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_route_route_code",
                table: "route",
                column: "route_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_route_stop_station_route",
                table: "route_stop",
                columns: new[] { "station_id", "route_id" });

            migrationBuilder.CreateIndex(
                name: "ux_route_stop_route_order",
                table: "route_stop",
                columns: new[] { "route_id", "stop_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calling_point_origin_destination",
                table: "service_calling_point",
                column: "route_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_operation_date_schedule",
                table: "service_operation_date",
                columns: new[] { "operation_date", "schedule_id" });

            migrationBuilder.CreateIndex(
                name: "ux_schedule_current_published_service",
                table: "service_schedule_revision",
                column: "service_id",
                unique: true,
                filter: "is_published = TRUE AND superseded_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_schedule_service_revision",
                table: "service_schedule_revision",
                columns: new[] { "service_id", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_station_source_station_key",
                table: "station",
                column: "source_station_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_station_change_import_run",
                table: "station_change",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_station_change_station_changed_at",
                table: "station_change",
                columns: new[] { "station_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_station_import_run_source_started",
                table: "station_import_run",
                columns: new[] { "source_revision", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_train_service_route_id",
                table: "train_service",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ux_train_service_service_code",
                table: "train_service",
                column: "service_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_calling_point");

            migrationBuilder.DropTable(
                name: "service_operation_date");

            migrationBuilder.DropTable(
                name: "station_change");

            migrationBuilder.DropTable(
                name: "route_stop");

            migrationBuilder.DropTable(
                name: "service_date_manifest");

            migrationBuilder.DropTable(
                name: "service_schedule_revision");

            migrationBuilder.DropTable(
                name: "station_import_run");

            migrationBuilder.DropTable(
                name: "station");

            migrationBuilder.DropTable(
                name: "train_service");

            migrationBuilder.DropTable(
                name: "route");
        }
    }
}
