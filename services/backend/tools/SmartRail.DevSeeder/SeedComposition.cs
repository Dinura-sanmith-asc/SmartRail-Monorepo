using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.JourneysStatus.Initialization;
using SmartRail.Application.RailwayNetwork.Services;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Infrastructure;
using SmartRail.Infrastructure.Incidents.Categories;

namespace SmartRail.DevSeeder;

// This composition lives only in the manually invoked tool. Normal API DI remains deny-by-default.
internal static class SeedComposition
{
    internal static ServiceProvider Create(string connection, Action<IServiceCollection>? configureForTests = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSmartRailInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SmartRailDbContext"] = connection
        }).Build());
        services.RemoveAll<IStationImportAuthorization>();
        services.AddSingleton<IStationImportAuthorization, LocalSeedAuthority>();
        services.RemoveAll<ISchedulePublicationAuthorization>();
        services.AddSingleton<ISchedulePublicationAuthorization, LocalSeedAuthority>();
        services.RemoveAll<IIdentityMaintenanceAuthority>();
        services.AddSingleton<IIdentityMaintenanceAuthority, LocalSeedAuthority>();
        services.RemoveAll<IJourneyInitializationAuthority>();
        services.AddSingleton<IJourneyInitializationAuthority, LocalSeedAuthority>();
        services.Configure<IncidentCategoryCatalogOptions>(options => options.Codes = [SeedManifest.Category]);
        configureForTests?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private sealed class LocalSeedAuthority : IStationImportAuthorization, ISchedulePublicationAuthorization,
        IIdentityMaintenanceAuthority, IJourneyInitializationAuthority
    {
        public ValueTask<bool> IsAuthorizedAsync(string invocationIdentity, string submitter, string approver, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(invocationIdentity == SeedManifest.Actor && submitter == SeedManifest.Actor && approver == SeedManifest.Actor);
        public ValueTask<bool> IsAuthorizedAsync(string publisherIdentity, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(publisherIdentity == SeedManifest.Actor);
        public Task<MaintenanceAuthorityDecision> AuthorizeAsync(string actorSubject, MaintenanceAction action, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MaintenanceAuthorityDecision(actorSubject == SeedManifest.Actor ? MaintenanceAuthorityOutcome.Allowed : MaintenanceAuthorityOutcome.Denied));

        public Task<JourneyInitializationAuthorityResult> AuthorizeAsync(InitializeJourneyCommand command, CancellationToken cancellationToken)
        {
            var developmentSeed = command.SourceSystem == SeedManifest.Version &&
                command.Provenance == SeedManifest.Version && command.JourneyInstanceId.HasValue &&
                (Enumerable.Range(0, 6).Any(i => command.JourneyInstanceId == SeedManifest.Id(5, i)) ||
                 Enumerable.Range(0, 2).Any(i => command.JourneyInstanceId == SeedManifest.Id(13, i)));
            var stationDisplaySeed = command.SourceSystem == StationDisplaySeedManifest.SourceSystem &&
                command.SourceInstanceKey == StationDisplaySeedManifest.SourceInstanceKey &&
                command.Provenance == StationDisplaySeedManifest.Provenance &&
                command.OperationDate == StationDisplaySeedManifest.OperationDate &&
                command.JourneyInstanceId == StationDisplaySeedManifest.JourneyId &&
                command.ServiceId == StationDisplaySeedManifest.ServiceId &&
                command.ScheduleId == StationDisplaySeedManifest.ScheduleId &&
                command.ScheduleRevision == 1 &&
                command.FirstDepartureOccurrence == StationDisplaySeedManifest.ColomboDeparture;

            return Task.FromResult(developmentSeed || stationDisplaySeed
                ? new JourneyInitializationAuthorityResult(JourneyInitializationAuthorityStatus.Authorized, SeedManifest.Actor, command.SourceSystem)
                : new JourneyInitializationAuthorityResult(JourneyInitializationAuthorityStatus.Denied, null, null));
        }
    }
}
