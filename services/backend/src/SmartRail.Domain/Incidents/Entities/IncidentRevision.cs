using SmartRail.Domain.Incidents;

namespace SmartRail.Domain.Incidents.Entities;

/// <summary>Immutable server-authored evidence for one accepted incident revision.</summary>
public sealed class IncidentRevision
{
    private IncidentRevision() { }

    public Guid IncidentId { get; private set; }
    public int Revision { get; private set; }
    public IncidentState State { get; private set; }
    public IncidentState? PreviousState { get; private set; }
    public string ActorSubject { get; private set; } = string.Empty;
    public string Source { get; private set; } = "IncidentManagement";
    public DateTimeOffset OccurredAt { get; private set; }
    public string AssociationEvidence { get; private set; } = "[]";

    public IncidentRevision(Guid incidentId, int revision, IncidentState state, string actorSubject,
        DateTimeOffset occurredAt, string associationEvidence, IncidentState? previousState = null)
    {
        if (incidentId == Guid.Empty) throw new ArgumentException("Incident ID is required.", nameof(incidentId));
        if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision));
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        if (previousState.HasValue && (!Enum.IsDefined(previousState.Value) || !IncidentLifecycle.CanTransition(previousState.Value, state)))
            throw new ArgumentException("Revision transition evidence must match an approved lifecycle transition.", nameof(previousState));
        IncidentId = incidentId;
        Revision = revision;
        State = state;
        PreviousState = previousState;
        ActorSubject = Incident.Required(actorSubject, nameof(actorSubject), 300);
        OccurredAt = Incident.Utc(occurredAt, nameof(occurredAt));
        AssociationEvidence = Incident.Required(associationEvidence, nameof(associationEvidence), 4000);
    }
}
