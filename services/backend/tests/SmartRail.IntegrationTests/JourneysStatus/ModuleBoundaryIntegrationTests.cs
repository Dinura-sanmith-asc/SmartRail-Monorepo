using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.JourneysStatus.Boundaries;
using SmartRail.Infrastructure.JourneysStatus;
using SmartRail.Infrastructure.JourneysStatus.Authorization;
using SmartRail.Infrastructure.JourneysStatus.Boundaries;

namespace SmartRail.IntegrationTests.JourneysStatus;

public sealed class ModuleBoundaryIntegrationTests
{
    [Fact]
    public async Task Production_owner_boundary_registration_fails_closed_without_owner_modules()
    {
        var services = new ServiceCollection();
        services.AddJourneysStatusOwnerBoundaryServices();
        using var provider = services.BuildServiceProvider();

        var driver = provider.GetRequiredService<IDriverObservationVerifier>();
        var incident = provider.GetRequiredService<IIncidentJourneyContextReader>();
        var driverResult = await driver.VerifyAsync("verified-subject", Guid.NewGuid(), Guid.NewGuid());
        var incidentResult = await incident.GetAuthorizedContextAsync("verified-subject", Guid.NewGuid(), default);

        Assert.IsType<UnavailableDriverObservationVerifier>(driver);
        Assert.IsType<UnavailableIncidentJourneyContextReader>(incident);
        Assert.Equal(DriverObservationVerificationOutcome.Unavailable, driverResult.Outcome);
        Assert.Equal(IncidentJourneyContextStatus.Unavailable, incidentResult.Status);
        Assert.Empty(incidentResult.PublicSafeSummaries);
    }

    [Fact]
    public void Owner_boundary_registration_is_replaceable_only_by_explicit_host_registration()
    {
        var services = new ServiceCollection();
        services.AddJourneysStatusOwnerBoundaryServices();
        services.AddSingleton<IDriverObservationVerifier, TestOnlyVerifier>();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<TestOnlyVerifier>(provider.GetRequiredService<IDriverObservationVerifier>());
        Assert.IsType<UnavailableIncidentJourneyContextReader>(provider.GetRequiredService<IIncidentJourneyContextReader>());
    }

    private sealed class TestOnlyVerifier : IDriverObservationVerifier
    {
        public Task<DriverObservationVerification> VerifyAsync(string verifiedActorSubject, Guid observationId,
            Guid journeyInstanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DriverObservationVerification.Verified());
    }
}
