using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.RailwayNetwork.Routes;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.Infrastructure.RailwayNetwork.Persistence;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Infrastructure.Identity.Persistence;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Maintenance;
using SmartRail.Application.Identity.Queries;
using SmartRail.Infrastructure.Identity;
using SmartRail.Infrastructure.Identity.Maintenance;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Infrastructure.JourneysStatus.Persistence;
using SmartRail.Infrastructure.JourneysStatus.Initialization;

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
        services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        services.AddScoped<IStationScopeRepository, StationScopeRepository>();
        services.TryAddScoped<IDriverJourneyAssignmentEvidence, UnavailableOwnerEvidence>();
        services.TryAddScoped<IIncidentResourceEvidence, UnavailableOwnerEvidence>();
        services.TryAddScoped<IPassengerOrderOwnershipEvidence, UnavailableOwnerEvidence>();
        services.TryAddSingleton<IIdentityMaintenanceAuthority, DenyAllIdentityMaintenanceAuthority>();
        services.AddScoped<ResolveCurrentRolesHandler>();
        services.AddScoped<ResolveResourceScopeHandler>();
        services.AddScoped<AuthorizeOperationHandler>();
        services.AddScoped<GetCurrentIdentityHandler>();
        services.AddScoped<GrantRoleAssignmentHandler>();
        services.AddScoped<RevokeRoleAssignmentHandler>();
        services.AddScoped<GrantStationScopeHandler>();
        services.AddScoped<RevokeStationScopeHandler>();
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
        services.AddScoped<IJourneyRepository, JourneyRepository>();
        services.AddScoped<IJourneyUnitOfWork, JourneyUnitOfWork>();
        services.AddScoped<IJourneyInitializationRepository, JourneyInitializationRepository>();
        services.AddScoped<IJourneyInitializationAuthority, DenyAllJourneyInitializationAuthority>();
        services.AddScoped<IJourneyInstanceIdIssuer, JourneyInstanceIdIssuer>();
        services.AddScoped<IJourneyManifestValidator, JourneyManifestValidator>();
        services.AddScoped<IJourneyScheduleLinkVerifier, JourneyScheduleLinkVerifier>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<InitializeJourneyHandler>();
        return services;
    }
}
