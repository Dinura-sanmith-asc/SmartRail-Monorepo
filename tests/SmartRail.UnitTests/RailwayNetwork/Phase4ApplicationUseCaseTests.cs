using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;

namespace SmartRail.UnitTests.RailwayNetwork;

public sealed class Phase4ApplicationUseCaseTests
{
    [Fact]
    public async Task Route_handler_rejects_empty_id_without_repository_call()
    {
        var repository = new FakeRouteRepository();
        var result = await new GetRouteStopsHandler(repository).HandleAsync(new(Guid.Empty));
        Assert.True(result.IsInvalid);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Search_validation_distinguishes_invalid_pair_and_unpublished_station()
    {
        var stations = new FakeStationRepository();
        var validator = new SearchServicesValidator(stations);
        var same = Guid.NewGuid();
        stations.Published.Add(same);
        var identical = await validator.ValidateAsync(new(same, same, "2026-10-17"));
        Assert.False(identical.IsValid);
        Assert.Contains("destination", identical.Errors.Keys);

        var unpublished = await validator.ValidateAsync(new(Guid.NewGuid(), Guid.NewGuid(), "2026-10-17"));
        Assert.False(unpublished.IsValid);
        Assert.Contains("unknown or unpublished", unpublished.Errors["origin"][0]);

        var invalidDate = await validator.ValidateAsync(new(same, Guid.NewGuid(), "2026-02-30"));
        Assert.False(invalidDate.IsValid);
        Assert.Contains("date", invalidDate.Errors.Keys);
    }

    [Fact]
    public async Task Search_handler_keeps_empty_and_unavailable_distinct()
    {
        var stationRepository = new FakeStationRepository();
        var origin = Guid.NewGuid();
        var destination = Guid.NewGuid();
        stationRepository.Published.UnionWith([origin, destination]);
        var repository = new FakeSearchRepository();
        var handler = new SearchServicesHandler(new SearchServicesValidator(stationRepository), repository);
        var query = new SearchServicesQuery(origin, destination, "2026-10-17");

        var empty = await handler.HandleAsync(query);
        Assert.Equal(ServiceSearchOutcome.Available, empty.Outcome);
        Assert.Empty(empty.Services);

        repository.IsAvailable = false;
        var unavailable = await handler.HandleAsync(query);
        Assert.Equal(ServiceSearchOutcome.Unavailable, unavailable.Outcome);
    }

    [Fact]
    public async Task Station_import_deny_by_default_prevents_repository_mutation()
    {
        var repository = new FakeStationImportRepository();
        var handler = new ImportStationsHandler(new DenyAllStationImportAuthorization(), repository);
        var request = new StationImportRequest("r1", "hash", "submitter", "approver", DateTimeOffset.UtcNow, []);
        var result = await handler.HandleAsync(new("unknown", request));
        Assert.True(result.IsUnauthorized);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Authorized_station_import_reaches_repository()
    {
        var repository = new FakeStationImportRepository();
        var request = new StationImportRequest("r1", "hash", "submitter", "approver", DateTimeOffset.UtcNow, []);
        var handler = new ImportStationsHandler(new AllowStationImportAuthorization(), repository);
        var result = await handler.HandleAsync(new("approved-test-identity", request));
        Assert.False(result.IsUnauthorized);
        Assert.NotNull(result.ImportRun);
        Assert.Equal(1, repository.Calls);
    }

    [Fact]
    public async Task Timetable_publication_deny_by_default_prevents_repository_mutation()
    {
        var repository = new FakePublicationRepository();
        var handler = new PublishTimetableHandler(new DenyAllSchedulePublicationAuthorization(), repository);
        var request = new TimetablePublicationRequest(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
            "unknown", []);
        var result = await handler.HandleAsync(new(request));
        Assert.True(result.IsUnauthorized);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Authorized_timetable_publication_reaches_repository()
    {
        var repository = new FakePublicationRepository();
        var request = new TimetablePublicationRequest(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
            "approved-test-publisher", []);
        var handler = new PublishTimetableHandler(new AllowSchedulePublicationAuthorization(), repository);
        var result = await handler.HandleAsync(new(request));
        Assert.True(result.IsPublished);
        Assert.Equal(1, repository.Calls);
    }

    private sealed class FakeStationRepository : IStationRepository
    {
        public HashSet<Guid> Published { get; } = [];
        public Task<IReadOnlyList<StationView>> ListPublishedAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<StationView>>([]);
        public Task<bool> IsPublishedAsync(Guid stationId, CancellationToken cancellationToken = default) => Task.FromResult(Published.Contains(stationId));
    }

    private sealed class FakeRouteRepository : IRouteReadRepository
    {
        public int Calls { get; private set; }
        public Task<RouteStopsView?> GetPublishedStopsAsync(Guid routeId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<RouteStopsView?>(null);
        }
    }

    private sealed class FakeSearchRepository : IServiceSearchRepository
    {
        public bool IsAvailable { get; set; } = true;
        public Task<ServiceSearchRepositoryResult> SearchAsync(ServiceSearchCriteria query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ServiceSearchRepositoryResult(IsAvailable, []));
    }

    private sealed class FakeStationImportRepository : IStationImportRepository
    {
        public int Calls { get; private set; }
        public Task<SmartRail.Domain.RailwayNetwork.Entities.StationImportRun> ApplyAsync(StationImportRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new SmartRail.Domain.RailwayNetwork.Entities.StationImportRun(
                Guid.NewGuid(), request.SourceRevision, request.ContentHash, request.SubmittedBy, request.ApprovedBy,
                request.StartedAt, now, 0, 0, 0));
        }
    }

    private sealed class AllowStationImportAuthorization : IStationImportAuthorization
    {
        public ValueTask<bool> IsAuthorizedAsync(string invocationIdentity, string submitter, string approver, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(invocationIdentity == "approved-test-identity" && submitter != approver);
    }

    private sealed class AllowSchedulePublicationAuthorization : ISchedulePublicationAuthorization
    {
        public ValueTask<bool> IsAuthorizedAsync(string publisherIdentity, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(publisherIdentity == "approved-test-publisher");
    }

    private sealed class FakePublicationRepository : ISchedulePublicationRepository
    {
        public int Calls { get; private set; }
        public Task PublishTimetableAsync(TimetablePublicationRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
