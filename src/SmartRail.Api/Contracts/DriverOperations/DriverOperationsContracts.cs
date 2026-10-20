using System.Text.Json.Serialization;
using SmartRail.Application.DriverOperations;

namespace SmartRail.Api.Contracts.DriverOperations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SubmitDriverObservationRequest(Guid RequestId, string? Text);
public sealed record DriverAssignmentListResponse(IReadOnlyList<DriverAssignmentView> Items);
public sealed record DriverObservationListResponse(IReadOnlyList<DriverObservationView> Items);
