namespace SmartRail.Api.Contracts.RailwayNetwork;

public sealed record ProblemResponse(
    string Type,
    string Title,
    int Status,
    string? Detail = null,
    string? Instance = null,
    string? Code = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? TraceId = null);
