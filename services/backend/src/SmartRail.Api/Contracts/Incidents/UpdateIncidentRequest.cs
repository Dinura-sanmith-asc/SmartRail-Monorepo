using System.Text.Json.Serialization;
using SmartRail.Application.Incidents.UseCases;
using SmartRail.Domain.Incidents;

namespace SmartRail.Api.Contracts.Incidents;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateIncidentRequest(int? ExpectedRevision, string? State,
    IReadOnlyList<IncidentAssociationInput>? AddAssociations = null, IReadOnlyList<Guid>? RemoveAssociationIds = null)
{
    public UpdateIncidentInput ToInput()
    {
        IncidentState? parsed = State switch
        {
            null => null,
            "Reported" => IncidentState.Reported,
            "In Progress" => IncidentState.InProgress,
            "Resolved" => IncidentState.Resolved,
            "Closed" => IncidentState.Closed,
            _ => null
        };
        // Preserve an invalid non-empty external label as a null state so the application
        // rejects it without accepting internal enum spellings not present in the contract.
        var additions = AddAssociations?.Select(item =>
        {
            if (item is null || !Enum.TryParse<SmartRail.Application.Incidents.References.IncidentReferenceKind>(item.Kind, false, out var kind) || !Enum.IsDefined(kind))
                return new SmartRail.Application.Incidents.References.IncidentReference((SmartRail.Application.Incidents.References.IncidentReferenceKind)(-1), item?.ResourceId ?? Guid.Empty);
            return new SmartRail.Application.Incidents.References.IncidentReference(kind, item.ResourceId, item.UpdateId, item.Revision);
        }).ToArray();
        return new UpdateIncidentInput(ExpectedRevision, parsed, additions, RemoveAssociationIds);
    }
}
