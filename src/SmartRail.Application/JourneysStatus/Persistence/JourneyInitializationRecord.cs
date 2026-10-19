using SmartRail.Domain.JourneysStatus.Entities;

namespace SmartRail.Application.JourneysStatus.Persistence;

public sealed record JourneyInitializationRecord(Journey Journey, JourneyRevision Baseline,
    IReadOnlyCollection<JourneyRevisionStop> Stops, JourneyInitializationAudit AcceptedAudit,
    bool HasAuthoritativeJourneyId);

public enum JourneyInitializationStoreOutcome { Created, Replayed, Conflict }
public sealed record JourneyInitializationStoreResult(JourneyInitializationStoreOutcome Outcome, Journey? Journey);
