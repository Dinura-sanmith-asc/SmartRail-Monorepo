using System.Text.Json.Serialization;

namespace SmartRail.Api.Contracts.Incidents;

/// <summary>Owner reference supplied when adding an incident association.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IncidentAssociationInput(string? Kind, Guid ResourceId, Guid? UpdateId = null, int? Revision = null);
