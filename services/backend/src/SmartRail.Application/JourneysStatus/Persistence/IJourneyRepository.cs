using SmartRail.Domain.JourneysStatus.Entities;

namespace SmartRail.Application.JourneysStatus.Persistence;

public interface IJourneyRepository
{
    Task<Journey?> FindByIdAsync(Guid journeyInstanceId, CancellationToken cancellationToken);
    Task<int?> GetCurrentRevisionAsync(Guid journeyInstanceId, CancellationToken cancellationToken);
    Task<JourneyRevisionAppendResult> AppendRevisionAsync(Guid journeyInstanceId, int expectedRevision,
        JourneyRevision revision, IReadOnlyCollection<JourneyRevisionStop> stops, CancellationToken cancellationToken);
}

public enum JourneyRevisionAppendOutcome { Appended, NotFound, Stale }
public sealed record JourneyRevisionAppendResult(JourneyRevisionAppendOutcome Outcome, int? CurrentRevision);
