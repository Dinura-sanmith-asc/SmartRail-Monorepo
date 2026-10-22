namespace SmartRail.Application.RailwayNetwork.Stations;

public sealed record StationImportRequest(
    string SourceRevision,
    string ContentHash,
    string SubmittedBy,
    string ApprovedBy,
    DateTimeOffset StartedAt,
    IReadOnlyCollection<StationImportRecord> Stations);

public interface IStationImportRepository
{
    Task<SmartRail.Domain.RailwayNetwork.Entities.StationImportRun> ApplyAsync(
        StationImportRequest request, CancellationToken cancellationToken = default);
}
