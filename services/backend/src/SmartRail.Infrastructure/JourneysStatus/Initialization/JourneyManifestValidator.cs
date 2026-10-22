using SmartRail.Application.JourneysStatus.Initialization;

namespace SmartRail.Infrastructure.JourneysStatus.Initialization;

public sealed class JourneyManifestValidator : IJourneyManifestValidator
{
    public bool IsValid(string? sourceSystem, string? sourceInstanceKey, string? provenance) =>
        !string.IsNullOrWhiteSpace(sourceSystem) && sourceSystem.Trim().Length <= 100 &&
        !string.IsNullOrWhiteSpace(sourceInstanceKey) && sourceInstanceKey.Trim().Length <= 200 &&
        !string.IsNullOrWhiteSpace(provenance) && provenance.Trim().Length <= 1000 &&
        !provenance.Contains("password", StringComparison.OrdinalIgnoreCase) &&
        !provenance.Contains("token", StringComparison.OrdinalIgnoreCase);
}
