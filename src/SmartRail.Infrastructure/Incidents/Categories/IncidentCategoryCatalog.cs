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

        if (configuredCodes.Any(code => string.IsNullOrWhiteSpace(code) || code.Trim().Length > 100))
            return Task.FromResult(CategoryCheck.Unavailable());
        var normalized = configuredCodes.Select(code => code.Trim()).ToArray();
        var catalog = normalized.ToHashSet(StringComparer.Ordinal);
        if (catalog.Count != normalized.Length)
            return Task.FromResult(CategoryCheck.Unavailable());

        return Task.FromResult(catalog.Contains(categoryCode?.Trim() ?? string.Empty)
            ? CategoryCheck.Approved()
            : CategoryCheck.Unknown());
    }
}
