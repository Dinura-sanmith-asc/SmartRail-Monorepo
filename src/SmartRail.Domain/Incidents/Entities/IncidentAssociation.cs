using SmartRail.Domain.Incidents;

namespace SmartRail.Domain.Incidents.Entities;

public sealed class IncidentAssociation
{
    private IncidentAssociation() { }

    public Guid AssociationId { get; private set; }
    public Guid IncidentId { get; private set; }
    public IncidentAssociationKind Kind { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid? StationReferenceId { get; private set; }
    public Guid? ServiceReferenceId { get; private set; }
    public Guid? DisruptionUpdateId { get; private set; }
    public int? DisruptionRevision { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
    public int AddedRevision { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public int? RemovedRevision { get; private set; }
    public bool IsActive => RemovedRevision is null;

    public IncidentAssociation(Guid associationId, Guid incidentId, IncidentAssociationKind kind, Guid resourceId,
        DateTimeOffset addedAt, int addedRevision, Guid? disruptionUpdateId = null, int? disruptionRevision = null)
    {
        if (associationId == Guid.Empty || incidentId == Guid.Empty || resourceId == Guid.Empty)
            throw new ArgumentException("Association, incident, and resource IDs are required.");
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (addedRevision <= 0) throw new ArgumentOutOfRangeException(nameof(addedRevision));
        if (kind == IncidentAssociationKind.Disruption && (!disruptionUpdateId.HasValue || disruptionUpdateId == Guid.Empty || disruptionRevision is null or <= 0))
            throw new ArgumentException("Disruption associations require an UpdateId and positive revision.");
        if (kind != IncidentAssociationKind.Disruption && (disruptionUpdateId.HasValue || disruptionRevision.HasValue))
            throw new ArgumentException("Only disruption associations may carry disruption evidence.");
        AssociationId = associationId;
        IncidentId = incidentId;
        Kind = kind;
        ResourceId = resourceId;
        StationReferenceId = kind == IncidentAssociationKind.Station ? resourceId : null;
        ServiceReferenceId = kind == IncidentAssociationKind.Service ? resourceId : null;
        DisruptionUpdateId = disruptionUpdateId;
        DisruptionRevision = disruptionRevision;
        AddedAt = Incident.Utc(addedAt, nameof(addedAt));
        AddedRevision = addedRevision;
    }

    public void Remove(int expectedIncidentRevision, int removedRevision, DateTimeOffset removedAt)
    {
        if (!IsActive) throw new InvalidOperationException("Association is already removed.");
        if (expectedIncidentRevision < AddedRevision)
            throw new InvalidOperationException("Association revision is inconsistent with the expected incident revision.");
        if (removedRevision != checked(expectedIncidentRevision + 1)) throw new ArgumentOutOfRangeException(nameof(removedRevision));
        RemovedAt = Incident.Utc(removedAt, nameof(removedAt));
        if (RemovedAt < AddedAt) throw new ArgumentOutOfRangeException(nameof(removedAt), "Removal cannot precede association creation.");
        RemovedRevision = removedRevision;
    }
}
