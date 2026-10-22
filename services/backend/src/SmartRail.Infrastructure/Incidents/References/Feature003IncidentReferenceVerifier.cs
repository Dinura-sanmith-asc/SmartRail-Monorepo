using SmartRail.Application.Incidents.References;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Domain.JourneysStatus;

namespace SmartRail.Infrastructure.Incidents.References;

/// <summary>
/// Verifies incident links against the authoritative Feature 003 journey snapshot and publication history.
/// It is deliberately read-only and does not change journey status or create a publication.
/// </summary>
public sealed class Feature003IncidentReferenceVerifier(IJourneyReadRepository journeys) : IFeature003IncidentReferenceVerifier
{
    public async Task<IncidentReferenceCheck> VerifyAsync(IncidentReference reference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (reference.Kind is not (IncidentReferenceKind.Journey or IncidentReferenceKind.Disruption) ||
            reference.ResourceId == Guid.Empty)
            return new(IncidentReferenceOutcome.MissingOrInaccessible);
        if (reference.Kind == IncidentReferenceKind.Journey && (reference.UpdateId.HasValue || reference.Revision.HasValue))
            return new(IncidentReferenceOutcome.StaleOrInactive);
        if (reference.Kind == IncidentReferenceKind.Disruption &&
            (!reference.UpdateId.HasValue || reference.UpdateId == Guid.Empty || reference.Revision is null or <= 0))
            return new(IncidentReferenceOutcome.StaleOrInactive);

        try
        {
            var snapshot = await journeys.ReadSnapshotAsync(reference.ResourceId, cancellationToken);
            if (snapshot.Outcome == JourneyReadOutcome.Unavailable)
                return new(IncidentReferenceOutcome.Unavailable);
            if (snapshot.Outcome == JourneyReadOutcome.NotFound)
                return new(IncidentReferenceOutcome.MissingOrInaccessible);
            if (snapshot.Journey is null || snapshot.Revision is null ||
                snapshot.Journey.JourneyInstanceId != reference.ResourceId ||
                snapshot.Revision.JourneyInstanceId != reference.ResourceId ||
                snapshot.Revision.Revision != snapshot.Journey.CurrentRevision)
                return new(IncidentReferenceOutcome.Unavailable);

            if (!IsActive(snapshot.Revision.Status))
                return new(IncidentReferenceOutcome.StaleOrInactive);

            if (reference.Kind == IncidentReferenceKind.Journey)
                return new(IncidentReferenceOutcome.Verified);

            var history = await journeys.ReadHistoryAsync(reference.ResourceId, cancellationToken);
            if (history.Outcome == JourneyReadOutcome.Unavailable)
                return new(IncidentReferenceOutcome.Unavailable);
            if (history.Outcome == JourneyReadOutcome.NotFound)
                return new(IncidentReferenceOutcome.MissingOrInaccessible);
            if (history.Revisions is null)
                return new(IncidentReferenceOutcome.Unavailable);
            var latestHistoryRevision = history.Revisions.OrderByDescending(item => item.Revision).FirstOrDefault();
            if (latestHistoryRevision is null || latestHistoryRevision.Revision != snapshot.Journey.CurrentRevision ||
                latestHistoryRevision.Status != snapshot.Revision.Status)
                return new(IncidentReferenceOutcome.Unavailable);

            var matchingUpdate = history.Revisions
                .Where(item => item.JourneyInstanceId == reference.ResourceId && item.UpdateId == reference.UpdateId)
                .ToArray();
            if (matchingUpdate.Length == 0)
                return new(IncidentReferenceOutcome.MissingOrInaccessible);
            if (matchingUpdate.Length != 1)
                return new(IncidentReferenceOutcome.Unavailable);

            var publication = matchingUpdate[0];
            if (publication.PublicationKind == JourneyPublicationKind.InitializationBaseline)
                return new(IncidentReferenceOutcome.MissingOrInaccessible);
            if (publication.Revision != reference.Revision)
                return new(IncidentReferenceOutcome.StaleOrInactive);

            return new(IncidentReferenceOutcome.Verified);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Owner data is authoritative. Any unexpected read/consistency failure must fail closed.
            return new(IncidentReferenceOutcome.Unavailable);
        }
    }

    private static bool IsActive(JourneyStatus status) => status is not
        (JourneyStatus.Cancelled or JourneyStatus.Completed or JourneyStatus.Terminated);
}
