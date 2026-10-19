using SmartRail.Domain.JourneysStatus;
using SmartRail.Domain.JourneysStatus.Entities;
using SmartRail.Application.JourneysStatus.Projections;

namespace SmartRail.Application.JourneysStatus.Queries;

public sealed record GetJourneyUpdatesQuery(Guid JourneyInstanceId);

public sealed class GetJourneyUpdatesHandler(IJourneyReadRepository repository)
{
    public async Task<JourneyReadResult<IReadOnlyList<JourneyUpdateView>>> HandleAsync(
        GetJourneyUpdatesQuery query, CancellationToken cancellationToken = default)
    {
        var result = await repository.ReadHistoryAsync(query.JourneyInstanceId, cancellationToken);
        if (result.Outcome != JourneyReadOutcome.Found) return new(result.Outcome);
        var revisions = result.Revisions ?? Array.Empty<JourneyRevision>();
        var stopsByRevision = result.StopsByRevision ?? new Dictionary<int, IReadOnlyList<JourneyRevisionStop>>();
        var official = revisions.Where(item => item.UpdateId.HasValue &&
                item.PublicationKind != JourneyPublicationKind.InitializationBaseline)
            .OrderBy(item => item.Revision).ToArray();
        var byUpdate = official.ToDictionary(item => item.UpdateId!.Value);
        var views = new List<JourneyUpdateView>(official.Length);
        foreach (var revision in official)
        {
            var stops = stopsByRevision.TryGetValue(revision.Revision, out var current)
                ? current : Array.Empty<JourneyRevisionStop>();
            var publicChange = CreateChange(revision, stops, revisions, stopsByRevision, byUpdate, new HashSet<Guid>());
            var kind = revision.PublicationKind switch
            {
                JourneyPublicationKind.StopChange => "stopChange",
                JourneyPublicationKind.RouteDiversion => "routeDiversion",
                JourneyPublicationKind.Correction => "correction",
                _ => "status"
            };
            views.Add(new JourneyUpdateView(revision.UpdateId!.Value, revision.JourneyInstanceId,
                revision.Revision, kind, revision.Status.ToString(), revision.Source.ToString(),
                revision.PublishedAt, revision.CorrectionOfUpdateId, publicChange));
        }
        return new(JourneyReadOutcome.Found, views);
    }

    private static JourneyPublicChangeView CreateChange(JourneyRevision revision,
        IReadOnlyList<JourneyRevisionStop> currentStops, IReadOnlyList<JourneyRevision> revisions,
        IReadOnlyDictionary<int, IReadOnlyList<JourneyRevisionStop>> stopsByRevision,
        IReadOnlyDictionary<Guid, JourneyRevision> byUpdate, HashSet<Guid> resolving)
    {
        switch (revision.PublicationKind)
        {
            case JourneyPublicationKind.Status:
                return new StatusPublicChangeView(revision.Status.ToString());
            case JourneyPublicationKind.StopChange:
            {
                var priorStops = revision.PreviousRevision.HasValue && stopsByRevision.TryGetValue(revision.PreviousRevision.Value, out var previous)
                    ? previous : Array.Empty<JourneyRevisionStop>();
                var changed = currentStops.OrderBy(item => item.StopOrder).FirstOrDefault(stop =>
                {
                    var prior = priorStops.FirstOrDefault(item => item.JourneyStopId == stop.JourneyStopId);
                    return prior is null || prior.Platform != stop.Platform ||
                           prior.OperationalArrivalAt != stop.OperationalArrivalAt ||
                           prior.OperationalDepartureAt != stop.OperationalDepartureAt;
                });
                if (changed is null) throw new JourneyReadProjectionException("Published stop change has no identifiable stop delta.");
                var priorStop = priorStops.FirstOrDefault(item => item.JourneyStopId == changed.JourneyStopId);
                return new StopPublicChangeView(changed.JourneyStopId,
                    priorStop?.Platform == changed.Platform ? null : changed.Platform,
                    priorStop?.OperationalArrivalAt == changed.OperationalArrivalAt ? null : changed.OperationalArrivalAt,
                    priorStop?.OperationalDepartureAt == changed.OperationalDepartureAt ? null : changed.OperationalDepartureAt);
            }
            case JourneyPublicationKind.RouteDiversion:
                return new RouteDiversionPublicChangeView(currentStops.OrderBy(item => item.StopOrder)
                    .Select(stop => new DivertedStopView(stop.JourneyStopId, stop.StationId, stop.StopOrder,
                        stop.PlannedArrivalAt, stop.PlannedDepartureAt, stop.OperationalArrivalAt,
                        stop.OperationalDepartureAt, stop.Platform)).ToArray());
            case JourneyPublicationKind.Correction:
            {
                var targetId = revision.CorrectionOfUpdateId;
                if (!targetId.HasValue || !byUpdate.TryGetValue(targetId.Value, out var target) ||
                    target.JourneyInstanceId != revision.JourneyInstanceId || target.Revision >= revision.Revision ||
                    !resolving.Add(targetId.Value))
                    throw new JourneyReadProjectionException("Correction does not reference an earlier public update on this journey.");
                var targetStops = stopsByRevision.TryGetValue(target.Revision, out var targetRevisionStops)
                    ? targetRevisionStops : Array.Empty<JourneyRevisionStop>();
                var change = CreateChange(target, targetStops, revisions, stopsByRevision, byUpdate, resolving);
                resolving.Remove(targetId.Value);
                if (change is CorrectionPublicChangeView correction) change = correction.CorrectedChange;
                return new CorrectionPublicChangeView(change);
            }
            default:
                throw new JourneyReadProjectionException("Initialization baseline cannot be exposed as an operational update.");
        }
    }
}
