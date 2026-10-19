namespace SmartRail.Api.Endpoints.JourneysStatus;

public static class JourneyReadEndpointRegistration
{
    public static IEndpointRouteBuilder MapJourneysStatusReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetJourney();
        endpoints.MapGetJourneyUpdates();
        return endpoints;
    }
}
