using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;

namespace SmartRail.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartRailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(SmartRailDbContext.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{SmartRailDbContext.ConnectionStringName}' is required. " +
                "Set it with .NET User Secrets or the ConnectionStrings__SmartRailDbContext environment variable.");
        }

        services.AddDbContext<SmartRailDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IStationRepository, StationReadRepository>();
        services.AddScoped<IRouteReadRepository, RouteReadRepository>();
        services.AddScoped<IServiceReadRepository, ServiceReadRepository>();
        services.AddScoped<IServiceSearchRepository, ServiceSearchRepository>();
        services.AddScoped<IStationImportRepository, StationImportRepository>();
        services.AddScoped<ISchedulePublicationRepository, SchedulePublicationRepository>();
        services.TryAddSingleton<IStationImportAuthorization, DenyAllStationImportAuthorization>();
        services.TryAddSingleton<ISchedulePublicationAuthorization, DenyAllSchedulePublicationAuthorization>();
        services.AddScoped<SearchServicesValidator>();
        services.AddScoped<ListStationsHandler>();
        services.AddScoped<GetRouteStopsHandler>();
        services.AddScoped<GetServiceHandler>();
        services.AddScoped<SearchServicesHandler>();
        services.AddScoped<ImportStationsHandler>();
        services.AddScoped<PublishTimetableHandler>();
        return services;
    }
}
