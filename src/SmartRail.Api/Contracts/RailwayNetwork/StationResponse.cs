using System.Text.Json.Serialization;

namespace SmartRail.Api.Contracts.RailwayNetwork;

public sealed record StationResponse(Guid StationId, string Name)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? LocationDetails { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? ServiceDetails { get; init; }
}
public sealed record StationListResponse(IReadOnlyList<StationResponse> Items);
