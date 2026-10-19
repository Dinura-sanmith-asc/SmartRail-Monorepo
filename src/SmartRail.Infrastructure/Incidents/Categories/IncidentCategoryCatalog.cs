using Microsoft.Extensions.Options;
using SmartRail.Application.Incidents.Categories;

namespace SmartRail.Infrastructure.Incidents.Categories;

/// <summary>Resolves only category codes explicitly supplied by configuration.</summary>
public sealed class IncidentCategoryCatalog(IOptionsMonitor<IncidentCategoryCatalogOptions> options) : IIncidentCategoryCatalog
{
    public Task<CategoryCheck> CheckAsync(string categoryCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuredCodes = options.CurrentValue.Codes;
        if (configuredCodes is null || configuredCodes.Length == 0)
            return Task.FromResult(CategoryCheck.Unavailable());

        var normalized = configuredCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .ToHashSet(StringComparer.Ordinal);
        if (normalized.Count == 0)
            return Task.FromResult(CategoryCheck.Unavailable());

        return Task.FromResult(normalized.Contains(categoryCode?.Trim() ?? string.Empty)
            ? CategoryCheck.Approved()
            : CategoryCheck.Unknown());
    }
}
