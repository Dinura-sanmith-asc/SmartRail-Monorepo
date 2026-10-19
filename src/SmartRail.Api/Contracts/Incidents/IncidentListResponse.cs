namespace SmartRail.Api.Contracts.Incidents;

/// <summary>The authorized incident collection returned by GET /api/incidents.</summary>
public sealed record IncidentListResponse(IReadOnlyList<IncidentResponse> Items);
