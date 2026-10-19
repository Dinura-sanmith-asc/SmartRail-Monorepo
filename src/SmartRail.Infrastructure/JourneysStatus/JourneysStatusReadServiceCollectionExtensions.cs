using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Application.JourneysStatus.Queries.GetStationDepartures;
using SmartRail.Infrastructure.JourneysStatus.Persistence;

namespace SmartRail.Infrastructure.JourneysStatus;

public static class JourneysStatusReadServiceCollectionExtensions
{
    public static IServiceCollection AddJourneysStatusReadServices(this IServiceCollection services)
    {
        services.AddScoped<IJourneyReadRepository, JourneyReadRepository>();
        services.AddScoped<GetJourneyHandler>();
        services.AddScoped<GetJourneyUpdatesHandler>();
        services.AddScoped<GetStationDeparturesHandler>();
        return services;
    }
}
