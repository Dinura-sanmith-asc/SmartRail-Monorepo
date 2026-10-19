namespace SmartRail.Domain.JourneysStatus.Rules;

public static class JourneyInitializationRules
{
    public static bool IsIdempotentReplay(string existingSourceSystem, string existingSourceKey,
        string requestedSourceSystem, string requestedSourceKey, Guid existingJourneyId, Guid requestedJourneyId,
        Guid existingServiceId, Guid requestedServiceId, Guid existingScheduleId, Guid requestedScheduleId,
        int existingScheduleRevision, int requestedScheduleRevision, DateOnly existingOperationDate,
        DateOnly requestedOperationDate, TimeOnly existingOccurrence, TimeOnly requestedOccurrence,
        bool hasAuthoritativeRequestedJourneyId)
    {
        return string.Equals(existingSourceSystem, requestedSourceSystem?.Trim(), StringComparison.Ordinal) &&
               string.Equals(existingSourceKey, requestedSourceKey?.Trim(), StringComparison.Ordinal) &&
               (!hasAuthoritativeRequestedJourneyId || existingJourneyId == requestedJourneyId) && existingServiceId == requestedServiceId &&
               existingScheduleId == requestedScheduleId && existingScheduleRevision == requestedScheduleRevision &&
               existingOperationDate == requestedOperationDate && existingOccurrence == requestedOccurrence;
    }

    public static void ValidateSourceIdentity(string? sourceSystem, string? sourceInstanceKey, Guid? journeyInstanceId)
    {
        if (string.IsNullOrWhiteSpace(sourceSystem) || sourceSystem.Trim().Length > 100)
            throw new ArgumentException("A bounded source system is required.", nameof(sourceSystem));
        if (string.IsNullOrWhiteSpace(sourceInstanceKey) || sourceInstanceKey.Trim().Length > 200)
            throw new ArgumentException("A bounded stable source instance key is required.", nameof(sourceInstanceKey));
        if (journeyInstanceId == Guid.Empty)
            throw new ArgumentException("Authoritative journey ID must be non-empty when supplied.", nameof(journeyInstanceId));
    }
}
