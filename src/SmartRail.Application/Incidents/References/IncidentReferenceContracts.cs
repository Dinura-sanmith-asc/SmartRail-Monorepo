namespace SmartRail.Application.Incidents.References;

public enum IncidentReferenceKind { Station, Service, Journey, Disruption }
public enum IncidentReferenceOutcome { Verified, MissingOrInaccessible, StaleOrInactive, Unavailable }
public sealed record IncidentReference(IncidentReferenceKind Kind, Guid ResourceId, Guid? UpdateId = null, int? Revision = null);
public sealed record IncidentReferenceCheck(IncidentReferenceOutcome Outcome);

/// <summary>Feature 001 is authoritative for station/service references; Feature 003 for journey/disruption evidence.</summary>
public interface IIncidentReferenceVerifier
{
    Task<IncidentReferenceCheck> VerifyAsync(IncidentReference reference, CancellationToken cancellationToken = default);
}
