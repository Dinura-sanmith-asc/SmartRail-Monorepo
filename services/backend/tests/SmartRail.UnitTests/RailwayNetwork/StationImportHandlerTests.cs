using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.UnitTests.RailwayNetwork;

public sealed class StationImportHandlerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unauthorized-test-identity")]
    public async Task Missing_or_unauthorized_invocation_is_denied_before_repository_mutation(string? identity)
    {
        var repository = new RecordingRepository();
        var request = Request([new(Guid.NewGuid(), "SRC-1", "Station", true, new Dictionary<string, string>())]);
        var handler = new ImportStationsHandler(new TestAuthorization(), repository);

        var result = await handler.HandleAsync(new(identity, request));

        Assert.True(result.IsUnauthorized);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Authorized_identity_still_validates_the_entire_batch_before_repository_call()
    {
        var repository = new RecordingRepository();
        var duplicateRows = Request([
            new(Guid.NewGuid(), "SRC-DUP", "First", true, new Dictionary<string, string>()),
            new(Guid.NewGuid(), " SRC-DUP ", "Second", true, new Dictionary<string, string>())
        ]);
        var result = await new ImportStationsHandler(new TestAuthorization(), repository)
            .HandleAsync(new("authorized-test-identity", duplicateRows));

        Assert.False(result.IsUnauthorized);
        Assert.Null(result.ImportRun);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(0, repository.Calls);
    }

    private static StationImportRequest Request(IReadOnlyCollection<StationImportRecord> rows) =>
        new("test-revision", "sha256:test", "test-submitter", "test-approver", DateTimeOffset.UtcNow, rows);

    private sealed class TestAuthorization : IStationImportAuthorization
    {
        public ValueTask<bool> IsAuthorizedAsync(string invocationIdentity, string submitter, string approver,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(invocationIdentity == "authorized-test-identity" && submitter != approver);
    }

    private sealed class RecordingRepository : IStationImportRepository
    {
        public int Calls { get; private set; }

        public Task<StationImportRun> ApplyAsync(StationImportRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new StationImportRun(Guid.NewGuid(), request.SourceRevision, request.ContentHash,
                request.SubmittedBy, request.ApprovedBy, request.StartedAt, now, request.Stations.Count, 0, 0));
        }
    }
}
