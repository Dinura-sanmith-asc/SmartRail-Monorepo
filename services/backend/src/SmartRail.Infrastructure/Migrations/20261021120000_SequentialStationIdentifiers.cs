using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartRail.Infrastructure.RailwayNetwork;

#nullable disable

namespace SmartRail.Infrastructure.Migrations;

[DbContext(typeof(SmartRailDbContext))]
[Migration("20261021120000_SequentialStationIdentifiers")]
public sealed class SequentialStationIdentifiers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE SEQUENCE station_id_number_seq AS bigint MINVALUE 1 MAXVALUE 999999999999 NO CYCLE;");
        migrationBuilder.Sql("""
            CREATE TEMP TABLE station_id_reorganization (
                old_id uuid PRIMARY KEY,
                new_id uuid NOT NULL UNIQUE
            ) ON COMMIT DROP;

            INSERT INTO station_id_reorganization (old_id, new_id)
            SELECT station_id,
                   ('10000000-0000-0000-0000-' || lpad((row_number() OVER (
                       ORDER BY CASE lower(btrim(name)) WHEN 'colombo fort' THEN 0 WHEN 'galle' THEN 1 ELSE 2 END,
                                CASE WHEN lower(btrim(name)) IN ('colombo fort', 'galle') THEN 0 ELSE 1 END,
                                lower(btrim(name)), station_id
                   ))::text, 12, '0'))::uuid
            FROM station;

            CREATE TEMP TABLE station_fk_reorganization (
                schema_name text NOT NULL,
                table_name text NOT NULL,
                constraint_name text NOT NULL,
                definition text NOT NULL,
                column_name text NOT NULL
            ) ON COMMIT DROP;

            DO $$
            DECLARE fk record;
            BEGIN
                FOR fk IN
                    SELECT ns.nspname AS schema_name, tbl.relname AS table_name, con.conname AS constraint_name,
                           pg_get_constraintdef(con.oid) AS definition, att.attname AS column_name
                    FROM pg_constraint con
                    JOIN pg_class tbl ON tbl.oid = con.conrelid
                    JOIN pg_namespace ns ON ns.oid = tbl.relnamespace
                    JOIN pg_attribute att ON att.attrelid = con.conrelid AND att.attnum = con.conkey[1]
                    WHERE con.contype = 'f' AND con.confrelid = 'station'::regclass
                LOOP
                    INSERT INTO station_fk_reorganization VALUES
                        (fk.schema_name, fk.table_name, fk.constraint_name, fk.definition, fk.column_name);
                    EXECUTE format('ALTER TABLE %I.%I DROP CONSTRAINT %I', fk.schema_name, fk.table_name, fk.constraint_name);
                END LOOP;
            END $$;

            ALTER TABLE station DROP CONSTRAINT pk_station;

            DO $$
            DECLARE fk record;
            BEGIN
                FOR fk IN SELECT * FROM station_fk_reorganization LOOP
                    IF fk.table_name = 'incident_association' AND fk.column_name = 'station_reference_id' THEN
                        CONTINUE;
                    END IF;
                    EXECUTE format(
                        'UPDATE %I.%I child SET %I = mapping.new_id FROM station_id_reorganization mapping WHERE child.%I = mapping.old_id',
                        fk.schema_name, fk.table_name, fk.column_name, fk.column_name);
                END LOOP;
            END $$;

            UPDATE incident_association association
            SET station_reference_id = mapping.new_id, resource_id = mapping.new_id
            FROM station_id_reorganization mapping
            WHERE association.kind = 'Station' AND association.station_reference_id = mapping.old_id;

            UPDATE station station_row
            SET station_id = mapping.new_id
            FROM station_id_reorganization mapping
            WHERE station_row.station_id = mapping.old_id;

            ALTER TABLE station ADD CONSTRAINT pk_station PRIMARY KEY (station_id);

            DO $$
            DECLARE fk record;
            BEGIN
                FOR fk IN SELECT * FROM station_fk_reorganization LOOP
                    EXECUTE format('ALTER TABLE %I.%I ADD CONSTRAINT %I %s',
                        fk.schema_name, fk.table_name, fk.constraint_name, fk.definition);
                END LOOP;
            END $$;

            SELECT setval('station_id_number_seq', GREATEST((SELECT count(*) FROM station), 2), true);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Sequential station ID reassignment cannot be rolled back without restoring the previous IDs from a backup.");
}
