using SmartRail.Domain.JourneysStatus.Entities;

namespace SmartRail.Application.JourneysStatus.Queries;

/// <summary>Read-only port for internally consistent passenger projections.</summary>
public interface IJourneyReadRepository
{
    Task<JourneySnapshotReadResult> ReadSnapshotAsync(Guid journeyInstanceId, CancellationToken cancellationToken);
    Task<JourneyHistoryReadResult> ReadHistoryAsync(Guid journeyInstanceId, CancellationToken cancellationToken);
    Task<JourneyStationDeparturesReadResult> ReadStationDeparturesAsync(Guid stationId, CancellationToken cancellationToken);
}

public enum JourneyReadOutcome { Found, NotFound, Unavailable }

public sealed record JourneySnapshotReadResult(JourneyReadOutcome Outcome, Journey? Journey = null,
    JourneyRevision? Revision = null, IReadOnlyList<JourneyRevisionStop>? Stops = null,
    IReadOnlyDictionary<Guid, string>? PublishedStationNames = null);

public sealed record JourneyHistoryReadResult(JourneyReadOutcome Outcome,
    IReadOnlyList<JourneyRevision>? Revisions = null,
    IReadOnlyDictionary<int, IReadOnlyList<JourneyRevisionStop>>? StopsByRevision = null);

public sealed record JourneyStationDeparturesReadResult(JourneyReadOutcome Outcome,
    IReadOnlyList<JourneyStationDepartureCandidate>? Candidates = null);

public sealed record JourneyStationDepartureCandidate(Journey Journey, JourneyRevision Revision,
    JourneyRevisionStop Stop, string StationName);
