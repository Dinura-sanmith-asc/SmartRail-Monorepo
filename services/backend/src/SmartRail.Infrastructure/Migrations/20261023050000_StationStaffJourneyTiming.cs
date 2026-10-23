using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartRail.Infrastructure.RailwayNetwork;

#nullable disable

namespace SmartRail.Infrastructure.Migrations;

[DbContext(typeof(SmartRailDbContext))]
[Migration("20261023050000_StationStaffJourneyTiming")]
public sealed class StationStaffJourneyTiming : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("ck_journey_revision_source", "journey_revision");
        migrationBuilder.AddCheckConstraint(
            "ck_journey_revision_source", "journey_revision",
            "(publication_kind = 'InitializationBaseline' AND source = 'ScheduledBaseline') OR (publication_kind <> 'InitializationBaseline' AND source IN ('ControllerManual', 'StationStaffManual', 'ApprovedRailwayFeed'))");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("ck_journey_revision_source", "journey_revision");
        migrationBuilder.AddCheckConstraint(
            "ck_journey_revision_source", "journey_revision",
            "(publication_kind = 'InitializationBaseline' AND source = 'ScheduledBaseline') OR (publication_kind <> 'InitializationBaseline' AND source IN ('ControllerManual', 'ApprovedRailwayFeed'))");
    }
}
