namespace SmartRail.Application.RailwayNetwork.Services;

public sealed record GetServiceQuery(Guid ServiceId);
public sealed record GetServiceResult(ServiceDetailView? Service, bool IsInvalid, string? Error);

public sealed class GetServiceHandler(IServiceReadRepository repository)
{
    public async Task<GetServiceResult> HandleAsync(GetServiceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.ServiceId == Guid.Empty) return new(null, true, "Service ID is required.");
        return new(await repository.GetPublishedAsync(query.ServiceId, cancellationToken), false, null);
    }
}
