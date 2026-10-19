namespace SmartRail.Application.JourneysStatus.Boundaries;

public enum IncidentJourneyContextStatus { Available, NoContext, Unavailable }
public sealed record IncidentJourneyContextResult(IncidentJourneyContextStatus Status, IReadOnlyList<string> PublicSafeSummaries);
