using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Infrastructure.JourneysStatus.Authorization;
using SmartRail.Infrastructure.JourneysStatus.Boundaries;

namespace SmartRail.Infrastructure.JourneysStatus;

/// <summary>Registers fail-closed owner boundaries until their owning modules are available.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddJourneysStatusOwnerBoundaryServices(this IServiceCollection services)
    {
        services.TryAddScoped<IDriverObservationVerifier, UnavailableDriverObservationVerifier>();
        services.TryAddScoped<IIncidentJourneyContextReader, UnavailableIncidentJourneyContextReader>();
        return services;
    }
}
