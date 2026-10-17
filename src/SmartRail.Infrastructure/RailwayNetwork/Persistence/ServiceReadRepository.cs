using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Services;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class ServiceReadRepository(SmartRailDbContext dbContext) : IServiceReadRepository
{
    public Task<ServiceDetailView?> GetPublishedAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        (from service in dbContext.TrainServices.AsNoTracking()
         join route in dbContext.Routes.AsNoTracking() on service.RouteId equals route.RouteId
         join schedule in dbContext.ServiceScheduleRevisions.AsNoTracking() on service.ServiceId equals schedule.ServiceId
         where service.ServiceId == serviceId && service.IsPublished && route.IsPublished && schedule.IsPublished && schedule.SupersededAt == null
         select new ServiceDetailView(service.ServiceId, service.ServiceCode, route.RouteId, route.RouteCode,
             route.Name, schedule.Revision, schedule.FirstDepartureLocalTime))
        .SingleOrDefaultAsync(cancellationToken);
}
