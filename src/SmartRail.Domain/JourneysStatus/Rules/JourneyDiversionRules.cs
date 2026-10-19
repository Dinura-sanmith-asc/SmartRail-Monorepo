namespace SmartRail.Domain.JourneysStatus.Rules;

public sealed record DiversionStopIdentity(Guid? JourneyStopId, int StopOrder);

public static class JourneyDiversionRules
{
    public static void Validate(IReadOnlyCollection<DiversionStopIdentity> replacement,
        IReadOnlySet<Guid> currentJourneyStopIds)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(currentJourneyStopIds);
        if (replacement.Count == 0) throw new ArgumentException("A diversion must contain at least one stop.", nameof(replacement));
        if (replacement.Any(stop => stop.StopOrder <= 0)) throw new ArgumentException("Stop order must be positive.", nameof(replacement));
        if (replacement.Select(stop => stop.StopOrder).Distinct().Count() != replacement.Count)
            throw new ArgumentException("Diversion stop order values must be unique.", nameof(replacement));
        var retainedIds = replacement.Where(stop => stop.JourneyStopId.HasValue).Select(stop => stop.JourneyStopId!.Value).ToArray();
        if (retainedIds.Any(id => id == Guid.Empty) || retainedIds.Distinct().Count() != retainedIds.Length)
            throw new ArgumentException("Retained stop IDs must be non-empty and unique.", nameof(replacement));
        if (retainedIds.Any(id => !currentJourneyStopIds.Contains(id)))
            throw new ArgumentException("Retained stop ID does not belong to the current journey revision.", nameof(replacement));
    }
}
