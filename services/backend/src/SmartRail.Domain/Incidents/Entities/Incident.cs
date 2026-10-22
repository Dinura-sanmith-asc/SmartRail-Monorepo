using SmartRail.Domain.Incidents;

namespace SmartRail.Domain.Incidents.Entities;

public sealed class Incident
{
    private Incident() { }

    public Guid IncidentId { get; private set; }
    public string CategoryCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public IncidentState State { get; private set; }
    public int Revision { get; private set; }
    public string ReporterSubject { get; private set; } = string.Empty;
    public DateTimeOffset ReportedAt { get; private set; }
    public DateTimeOffset? OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public ICollection<IncidentRevision> Revisions { get; private set; } = new List<IncidentRevision>();
    public ICollection<IncidentAssociation> Associations { get; private set; } = new List<IncidentAssociation>();

    public Incident(Guid incidentId, string categoryCode, string description, string reporterSubject,
        DateTimeOffset reportedAt, DateTimeOffset? occurredAt = null)
    {
        if (incidentId == Guid.Empty) throw new ArgumentException("Incident ID is required.", nameof(incidentId));
        IncidentId = incidentId;
        CategoryCode = Required(categoryCode, nameof(categoryCode), 100);
        Description = Required(description, nameof(description), 4000);
        ReporterSubject = Required(reporterSubject, nameof(reporterSubject), 300);
        ReportedAt = Utc(reportedAt, nameof(reportedAt));
        OccurredAt = occurredAt.HasValue ? Utc(occurredAt.Value, nameof(occurredAt)) : null;
        State = IncidentState.Reported;
        Revision = 1;
        CreatedAt = ReportedAt;
        UpdatedAt = ReportedAt;
    }

    public int TransitionTo(IncidentState next, int expectedRevision, DateTimeOffset updatedAt)
    {
        if (expectedRevision != Revision) throw new InvalidOperationException("Incident revision is stale.");
        if (!IncidentLifecycle.CanTransition(State, next)) throw new InvalidOperationException("Incident state transition is not allowed.");
        State = next;
        Revision = checked(Revision + 1);
        UpdatedAt = Utc(updatedAt, nameof(updatedAt));
        return Revision;
    }

    /// <summary>Advances the aggregate revision for an association-only update.</summary>
    public int AdvanceRevisionForAssociationChange(int expectedRevision, DateTimeOffset updatedAt)
    {
        if (expectedRevision != Revision) throw new InvalidOperationException("Incident revision is stale.");
        Revision = checked(Revision + 1);
        UpdatedAt = Utc(updatedAt, nameof(updatedAt));
        return Revision;
    }

    internal static string Required(string value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximumLength)
            throw new ArgumentException($"{name} is required and must be at most {maximumLength} characters.", name);
        return value.Trim();
    }

    internal static DateTimeOffset Utc(DateTimeOffset value, string name)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
            throw new ArgumentException($"{name} must be a non-default UTC timestamp.", name);
        return value;
    }
}
