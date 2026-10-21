namespace SmartRail.Application.RailwayNetwork.Services;

public sealed record ServiceDetailView(
    Guid ServiceId, string ServiceCode, Guid RouteId, string RouteCode, string RouteName,
    int ScheduleRevision, TimeOnly FirstDepartureTimeLocal, string? DisplayName = null, string? PublicServiceNumber = null);

public interface IServiceReadRepository
{
    Task<ServiceDetailView?> GetPublishedAsync(Guid serviceId, CancellationToken cancellationToken = default);
}
