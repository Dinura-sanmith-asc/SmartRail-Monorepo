using SmartRail.Application.RailwayNetwork.Stations;
using System.Globalization;

namespace SmartRail.Application.RailwayNetwork.Services;

public sealed record SearchServicesQuery(Guid OriginStationId, Guid DestinationStationId, string? TravelDate);
public sealed record ServiceSearchCriteria(Guid OriginStationId, Guid DestinationStationId, DateOnly TravelDate);
public sealed record SearchCriteriaValidationResult(IReadOnlyDictionary<string, string[]> Errors, DateOnly? ParsedTravelDate)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed record ScheduledCallView(Guid StationId, string StationName, int StopOrder, DateTimeOffset ScheduledAt);
public sealed record ServiceChoiceView(
    Guid ServiceId, string ServiceCode, Guid RouteId, string RouteCode, int ScheduleRevision,
    ScheduledCallView Origin, ScheduledCallView Destination);
public sealed record ServiceSearchRepositoryResult(bool IsAvailable, IReadOnlyList<ServiceChoiceView> Services);
public enum ServiceSearchOutcome { Invalid, Available, Unavailable }
public sealed record SearchServicesResult(
    ServiceSearchOutcome Outcome,
    IReadOnlyList<ServiceChoiceView> Services,
    IReadOnlyDictionary<string, string[]> Errors);

public interface IServiceSearchRepository
{
    Task<ServiceSearchRepositoryResult> SearchAsync(ServiceSearchCriteria criteria, CancellationToken cancellationToken = default);
}

public sealed class SearchServicesValidator(IStationRepository stations)
{
    public async Task<SearchCriteriaValidationResult> ValidateAsync(SearchServicesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        DateOnly? parsedDate = null;
        if (query.OriginStationId == Guid.Empty) errors["origin"] = ["Origin station is required."];
        if (query.DestinationStationId == Guid.Empty) errors["destination"] = ["Destination station is required."];
        if (query.OriginStationId != Guid.Empty && query.OriginStationId == query.DestinationStationId)
            errors["destination"] = ["Origin and destination must be different stations."];
        if (!DateOnly.TryParseExact(query.TravelDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            errors["date"] = ["Travel date must be a valid calendar date in yyyy-MM-dd format."];
        else parsedDate = date;
        if (query.OriginStationId != Guid.Empty && !await stations.IsPublishedAsync(query.OriginStationId, cancellationToken))
            errors["origin"] = ["Origin station is unknown or unpublished."];
        if (query.DestinationStationId != Guid.Empty && !await stations.IsPublishedAsync(query.DestinationStationId, cancellationToken))
            errors["destination"] = ["Destination station is unknown or unpublished."];
        return new(errors, parsedDate);
    }
}

public sealed class SearchServicesHandler(SearchServicesValidator validator, IServiceSearchRepository repository)
{
    public async Task<SearchServicesResult> HandleAsync(SearchServicesQuery query, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid) return new(ServiceSearchOutcome.Invalid, [], validation.Errors);
        var result = await repository.SearchAsync(new(query.OriginStationId, query.DestinationStationId, validation.ParsedTravelDate!.Value), cancellationToken);
        return result.IsAvailable
            ? new(ServiceSearchOutcome.Available, result.Services, new Dictionary<string, string[]>())
            : new(ServiceSearchOutcome.Unavailable, [], new Dictionary<string, string[]>());
    }
}
