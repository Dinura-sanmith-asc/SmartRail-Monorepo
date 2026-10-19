using SmartRail.Application.JourneysStatus.Initialization;

namespace SmartRail.Infrastructure.JourneysStatus.Initialization;

public sealed class DenyAllJourneyInitializationAuthority : IJourneyInitializationAuthority
{
    public Task<JourneyInitializationAuthorityResult> AuthorizeAsync(InitializeJourneyCommand command,
        CancellationToken cancellationToken) =>
        Task.FromResult(new JourneyInitializationAuthorityResult(JourneyInitializationAuthorityStatus.Denied, null, null));
}
