namespace SmartRail.Infrastructure.Incidents.Categories;

/// <summary>
/// Values supplied by an explicitly approved deployment configuration. An empty
/// catalogue deliberately means that production incident creation is disabled.
/// Test-only values belong in the isolated API test host, never in appsettings.
/// </summary>
public sealed class IncidentCategoryCatalogOptions
{
    public const string SectionName = "IncidentManagement:CategoryCatalog";

    public string[] Codes { get; set; } = [];
}
