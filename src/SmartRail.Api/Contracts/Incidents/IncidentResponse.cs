using SmartRail.Application.Incidents.UseCases;

namespace SmartRail.Api.Contracts.Incidents;

public sealed record IncidentResponse(
    Guid IncidentId,
    string CategoryCode,
    string Description,
    string State,
    int Revision,
    string Source,
    DateTimeOffset ReportedAt,
    DateTimeOffset? OccurredAt,
    IReadOnlyList<IncidentAssociationResponse> Associations)
{
    public static IncidentResponse From(IncidentCreatedProjection projection) => new(
        projection.IncidentId,
        projection.CategoryCode,
        projection.Description,
        ToStateLabel(projection.State),
        projection.Revision,
        projection.Source,
        projection.ReportedAt,
        projection.OccurredAt,
        projection.Associations.Select(item => new IncidentAssociationResponse(
            item.AssociationId, item.Kind, item.ResourceId, item.UpdateId, item.Revision)).ToArray());

    /// <summary>Map persisted enum names to the exact approved API labels.</summary>
    public static string ToStateLabel(string state) => state switch
    {
        "InProgress" or "In Progress" => "In Progress",
        "Reported" => "Reported",
        "Resolved" => "Resolved",
        "Closed" => "Closed",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };
}

public sealed record IncidentAssociationResponse(Guid AssociationId, string Kind, Guid ResourceId, Guid? UpdateId, int? Revision);
