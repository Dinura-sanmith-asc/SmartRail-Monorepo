using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Application.RailwayNetwork.Services;

public sealed record CreateTrainServiceRequest(string ServiceCode, Guid RouteId, string? DisplayName, string? PublicServiceNumber, IReadOnlyList<Guid>? RouteStopIds = null);
public sealed record UpdateTrainServiceRequest(string ServiceCode, Guid RouteId, string? DisplayName, string? PublicServiceNumber, IReadOnlyList<Guid>? RouteStopIds = null);
public sealed record TrainServiceManagementView(Guid ServiceId, string ServiceCode, string? DisplayName,
    string? PublicServiceNumber, Guid RouteId, string RouteCode, string RouteName, bool IsPublished, IReadOnlyList<Guid> RouteStopIds);
public sealed record CallingPointInput(Guid RouteStopId, int? ArrivalOffsetMinutes, int? DepartureOffsetMinutes);
public sealed record ScheduleRevisionInput(TimeOnly FirstDepartureLocalTime, IReadOnlyList<CallingPointInput> CallingPoints);
public sealed record ScheduleCallingPointView(Guid RouteStopId, Guid StationId, string StationName, int StopOrder,
    int? ArrivalOffsetMinutes, int? DepartureOffsetMinutes);
public sealed record ScheduleOperationDateView(DateOnly OperationDate, bool IsManifestComplete, string? ManifestSourceRevision);
public sealed record ServiceDateManifestView(DateOnly OperationDate, string SourceRevision, string FixtureVersion,
    bool IsComplete, DateTimeOffset? PublishedAt, string? PublishedBy, string ContentHash, IReadOnlyList<Guid> ScheduleIds);
public sealed record ScheduleRevisionView(Guid ScheduleId, int Revision, TimeOnly FirstDepartureLocalTime,
    bool IsPublished, DateTimeOffset? PublishedAt, DateTimeOffset? SupersededAt,
    IReadOnlyList<ScheduleCallingPointView> CallingPoints, IReadOnlyList<ScheduleOperationDateView> OperationDates);

public interface ITrainServiceManagementRepository
{
    Task<IReadOnlyList<TrainServiceManagementView>> ListAsync(CancellationToken cancellationToken = default);
    Task<TrainServiceManagementView?> CreateAsync(CreateTrainServiceRequest request, CancellationToken cancellationToken = default);
    Task<TrainServiceManagementView?> UpdateAsync(Guid serviceId, UpdateTrainServiceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduleRevisionView>?> GetSchedulesAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<ScheduleRevisionView> CreateScheduleAsync(Guid serviceId, ScheduleRevisionInput request, CancellationToken cancellationToken = default);
    Task<ScheduleRevisionView?> UpdateScheduleAsync(Guid serviceId, Guid scheduleId, ScheduleRevisionInput request, CancellationToken cancellationToken = default);
    Task<bool> DeleteScheduleAsync(Guid serviceId, Guid scheduleId, CancellationToken cancellationToken = default);
    Task PublishScheduleAsync(Guid serviceId, Guid scheduleId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduleOperationDateView>?> SetOperationDatesAsync(Guid serviceId, Guid scheduleId, IReadOnlyList<DateOnly> dates, CancellationToken cancellationToken = default);
    Task<ServiceDateManifestView?> GetManifestAsync(DateOnly operationDate, CancellationToken cancellationToken = default);
    Task<ServiceDateManifestView> SetManifestAsync(ServiceDateManifest manifest, IReadOnlyList<Guid> scheduleIds, CancellationToken cancellationToken = default);
}
