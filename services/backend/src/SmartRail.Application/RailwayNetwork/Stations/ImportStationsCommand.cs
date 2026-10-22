using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Application.RailwayNetwork.Stations;

public interface IStationImportAuthorization
{
    ValueTask<bool> IsAuthorizedAsync(string invocationIdentity, string submitter, string approver, CancellationToken cancellationToken = default);
}

public sealed class DenyAllStationImportAuthorization : IStationImportAuthorization
{
    public ValueTask<bool> IsAuthorizedAsync(string invocationIdentity, string submitter, string approver, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(false);
}

public sealed record ImportStationsCommand(string? InvocationIdentity, StationImportRequest Request);
public sealed record ImportStationsResult(StationImportRun? ImportRun, bool IsUnauthorized, IReadOnlyList<string> Errors);

public sealed class ImportStationsHandler(
    IStationImportAuthorization authorization,
    IStationImportRepository repository)
{
    public async Task<ImportStationsResult> HandleAsync(ImportStationsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);
        if (string.IsNullOrWhiteSpace(command.InvocationIdentity) ||
            string.IsNullOrWhiteSpace(command.Request.SubmittedBy) || string.IsNullOrWhiteSpace(command.Request.ApprovedBy) ||
            !await authorization.IsAuthorizedAsync(command.InvocationIdentity, command.Request.SubmittedBy,
                command.Request.ApprovedBy, cancellationToken))
            return new(null, true, ["The station import invocation is not authorized."]);
        var validation = ImportStationsValidator.Validate(command.Request.Stations.ToArray());
        if (!validation.IsValid) return new(null, false, validation.Errors);
        var run = await repository.ApplyAsync(command.Request, cancellationToken);
        return new(run, false, []);
    }
}
