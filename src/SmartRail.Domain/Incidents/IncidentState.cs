namespace SmartRail.Domain.Incidents;

public enum IncidentState
{
    Reported = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}

public static class IncidentLifecycle
{
    public static bool CanTransition(IncidentState current, IncidentState next) =>
        (current, next) is (IncidentState.Reported, IncidentState.InProgress)
            or (IncidentState.InProgress, IncidentState.Resolved)
            or (IncidentState.Resolved, IncidentState.Closed);
}
