namespace SmartRail.Application.Incidents.Categories;

public enum CategoryCheckOutcome { Approved, Unknown, Unavailable }
public sealed record CategoryCheck(CategoryCheckOutcome Outcome)
{
    public static CategoryCheck Approved() => new(CategoryCheckOutcome.Approved);
    public static CategoryCheck Unknown() => new(CategoryCheckOutcome.Unknown);
    public static CategoryCheck Unavailable() => new(CategoryCheckOutcome.Unavailable);
}

/// <summary>Supplies only an explicitly approved incident category vocabulary.</summary>
public interface IIncidentCategoryCatalog
{
    Task<CategoryCheck> CheckAsync(string categoryCode, CancellationToken cancellationToken = default);
}
