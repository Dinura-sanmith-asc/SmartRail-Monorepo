namespace SmartRail.Application.JourneysStatus.Queries;

public sealed record JourneyReadResult<T>(JourneyReadOutcome Outcome, T? Value = default);
