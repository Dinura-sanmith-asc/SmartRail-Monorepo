using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Application.JourneysStatus.Persistence;
using SmartRail.Application.JourneysStatus.Publication;
using SmartRail.Infrastructure.JourneysStatus.Authorization;
using SmartRail.Infrastructure.JourneysStatus.Persistence;

namespace SmartRail.Infrastructure.JourneysStatus.Publication;

public static class JourneyPublicationServiceCollectionExtensions
{
    public static IServiceCollection AddJourneysStatusPublicationServices(this IServiceCollection services)
    {
        services.TryAddScoped<IJourneyPublicationAuthorization, Feature002JourneyPublicationAuthorization>();
        services.TryAddScoped<IDriverObservationVerifier, UnavailableDriverObservationVerifier>();
        services.AddScoped<IJourneyPublicationRepository, JourneyPublicationRepository>();
        services.AddScoped<JourneyPublicationHandler>();
        return services;
    }
}
