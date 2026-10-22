using SmartRail.Application.JourneysStatus.Initialization;

namespace SmartRail.Infrastructure.JourneysStatus.Initialization;

public sealed class JourneyInstanceIdIssuer : IJourneyInstanceIdIssuer
{
    public Guid Issue(string sourceSystem, string sourceInstanceKey, Guid? authoritativeId)
    {
        if (string.IsNullOrWhiteSpace(sourceSystem) || string.IsNullOrWhiteSpace(sourceInstanceKey))
            throw new ArgumentException("Journey identity requires a validated source key.");
        if (authoritativeId == Guid.Empty) throw new ArgumentException("Authoritative journey identity cannot be empty.", nameof(authoritativeId));
        return authoritativeId ?? Guid.NewGuid();
    }
}
