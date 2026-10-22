using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Domain.RailwayNetwork.Rules;

namespace SmartRail.UnitTests.RailwayNetwork;

public sealed class Phase2DomainRulesTests
{
    [Fact]
    public void Station_requires_identity_and_name_and_keeps_source_identity_internal()
    {
        var id = Guid.NewGuid();
        var station = new Station(id, "source-1", "  Central  ", verifiedDetails: new Dictionary<string, string> { ["district"] = "Colombo" });

        Assert.Equal(id, station.StationId);
        Assert.Equal("Central", station.Name);
        Assert.Equal("Colombo", station.VerifiedDetails["district"]);
        Assert.Null(typeof(Station).GetProperty("SourceStationKey"));
        Assert.Throws<ArgumentException>(() => new Station(Guid.NewGuid(), "source-2", " "));
    }

    [Fact]
    public void Route_stop_requires_positive_order_and_stable_references()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RouteStop(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0));
        Assert.Throws<ArgumentException>(() => new RouteStop(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 1));
    }

    [Fact]
    public void Calling_point_rejects_negative_or_reversed_times()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServiceCallingPoint(Guid.NewGuid(), Guid.NewGuid(), -1, 0));
        Assert.Throws<ArgumentException>(() => new ServiceCallingPoint(Guid.NewGuid(), Guid.NewGuid(), 20, 10));
    }

    [Fact]
    public void Schedule_revision_preserves_timetable_values_and_tracks_publication_history()
    {
        var created = DateTimeOffset.Parse("2026-10-01T08:00:00+05:30");
        var revision = new ServiceScheduleRevision(Guid.NewGuid(), Guid.NewGuid(), 1, new TimeOnly(23, 30), created);

        revision.Publish(created.AddHours(1));
        revision.Supersede(created.AddDays(1));

        Assert.Equal("Asia/Colombo", revision.TimeZoneId);
        Assert.False(revision.IsCurrentPublished);
        Assert.Throws<InvalidOperationException>(() => revision.Publish(created.AddDays(2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServiceScheduleRevision(Guid.NewGuid(), Guid.NewGuid(), 0, TimeOnly.MinValue, created));
    }

    [Fact]
    public void Complete_empty_manifest_is_valid_but_incomplete_manifest_is_not_search_evidence()
    {
        var complete = new ServiceDateManifest(new DateOnly(2026, 10, 1), "source-r1", "fixture-r1", true,
            DateTimeOffset.UtcNow, "publisher-1", "sha256:value");
        var incomplete = new ServiceDateManifest(new DateOnly(2026, 10, 2), "source-r1", "fixture-r1", false,
            null, null, "sha256:value");

        Assert.True(complete.IsComplete);
        Assert.False(incomplete.IsComplete);
    }

    [Fact]
    public void Origin_date_calculation_handles_same_day_midnight_and_multiple_day_offsets()
    {
        var requested = new DateOnly(2026, 10, 2);

        Assert.Equal(requested, ServiceSearchEligibility.CalculateOperationDate(requested, new TimeOnly(8, 0), 60));
        Assert.Equal(new DateOnly(2026, 10, 1), ServiceSearchEligibility.CalculateOperationDate(requested, new TimeOnly(23, 30), 60));
        Assert.Equal(new DateOnly(2026, 9, 30), ServiceSearchEligibility.CalculateOperationDate(requested, new TimeOnly(23, 30), 1500));
    }

    [Fact]
    public void Eligibility_distinguishes_unknown_complete_empty_and_matching_publication()
    {
        var serviceId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var requestedDate = new DateOnly(2026, 10, 2);
        var schedule = new ServiceScheduleRevision(scheduleId, serviceId, 1, new TimeOnly(23, 30), DateTimeOffset.UtcNow,
            isPublished: true, publishedAt: DateTimeOffset.UtcNow);
        var originStop = new RouteStop(Guid.NewGuid(), routeId, Guid.NewGuid(), 1);
        var destinationStop = new RouteStop(Guid.NewGuid(), routeId, Guid.NewGuid(), 2);
        var origin = new ServiceCallingPoint(scheduleId, originStop.RouteStopId, 60, 65);
        var destination = new ServiceCallingPoint(scheduleId, destinationStop.RouteStopId, 120, 125);
        var operationDate = ServiceSearchEligibility.CalculateOperationDate(requestedDate, schedule.FirstDepartureLocalTime, 60);
        var manifest = new ServiceDateManifest(operationDate, "source-r1", "fixture-r1", true,
            DateTimeOffset.UtcNow, "publisher-1", "sha256:value");

        Assert.Equal(ServiceSearchEligibilityStatus.Unavailable,
            ServiceSearchEligibility.Evaluate(requestedDate, schedule, null, null, origin, originStop, destination, destinationStop).Status);
        Assert.Equal(ServiceSearchEligibilityStatus.NotEligible,
            ServiceSearchEligibility.Evaluate(requestedDate, schedule, manifest, null, origin, originStop, destination, destinationStop).Status);
        Assert.Equal(ServiceSearchEligibilityStatus.Eligible,
            ServiceSearchEligibility.Evaluate(requestedDate, schedule, manifest, new ServiceOperationDate(scheduleId, operationDate), origin, originStop, destination, destinationStop).Status);
        Assert.Equal(ServiceSearchEligibilityStatus.NotEligible,
            ServiceSearchEligibility.Evaluate(requestedDate, schedule, manifest, new ServiceOperationDate(scheduleId, operationDate), destination, destinationStop, origin, originStop).Status);
    }

    [Fact]
    public void Publication_validator_withholds_ambiguous_repeated_station_calls()
    {
        var routeId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var repeatedStation = Guid.NewGuid();
        var route = new Route(routeId, "R1", "Route", isPublished: true);
        var service = new TrainService(serviceId, "S1", routeId, isPublished: true);
        var now = DateTimeOffset.UtcNow;
        var revision = new ServiceScheduleRevision(scheduleId, serviceId, 1, new TimeOnly(8, 0), now, true, now);
        var stops = new[]
        {
            new RouteStop(Guid.NewGuid(), routeId, repeatedStation, 1),
            new RouteStop(Guid.NewGuid(), routeId, Guid.NewGuid(), 2),
            new RouteStop(Guid.NewGuid(), routeId, repeatedStation, 3)
        };
        var calls = new[]
        {
            new ServiceCallingPoint(scheduleId, stops[0].RouteStopId, null, 0),
            new ServiceCallingPoint(scheduleId, stops[1].RouteStopId, 60, 65),
            new ServiceCallingPoint(scheduleId, stops[2].RouteStopId, 120, null)
        };

        var result = SchedulePublicationValidator.Validate(service, route, stops, [revision], calls);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("repeated station", StringComparison.OrdinalIgnoreCase));
    }
}
