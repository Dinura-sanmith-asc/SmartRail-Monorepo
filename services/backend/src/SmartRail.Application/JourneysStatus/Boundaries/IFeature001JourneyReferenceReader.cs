namespace SmartRail.Application.JourneysStatus.Boundaries;

public interface IFeature001JourneyReferenceReader
{
    Task<bool> IsPublishedJourneyReferenceAsync(Guid serviceId, Guid scheduleId, DateOnly operationDate, CancellationToken cancellationToken);
}
