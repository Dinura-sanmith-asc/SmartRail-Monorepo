using System.Text.Json.Serialization;
using SmartRail.Application.Incidents.References;
using SmartRail.Application.Incidents.UseCases;

namespace SmartRail.Api.Contracts.Incidents;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateIncidentRequest(
    string? CategoryCode,
    string? Description,
    DateTimeOffset? OccurredAt,
    IReadOnlyList<IncidentAssociationInput>? Associations)
{
    public CreateIncidentInput ToInput()
    {
        var associations = Associations?.Select(item =>
        {
            if (item is null || !Enum.TryParse<IncidentReferenceKind>(item.Kind, ignoreCase: false, out var kind) || !Enum.IsDefined(kind))
                return new IncidentReference((IncidentReferenceKind)(-1), item?.ResourceId ?? Guid.Empty);
            return new IncidentReference(kind, item.ResourceId, item.UpdateId, item.Revision);
        }).ToArray();
        return new CreateIncidentInput(CategoryCode, Description, OccurredAt, associations);
    }
}
