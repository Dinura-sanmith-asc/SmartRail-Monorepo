namespace SmartRail.Application.DriverOperations;

public enum DriverAssignmentMaintenanceAction { Grant, Revoke }
public enum DriverAssignmentMaintenanceOutcome { Allowed, Denied, Unavailable }

/// <summary>Restricted non-HTTP maintenance, never inferred from an ordinary role.</summary>
public interface IDriverAssignmentMaintenanceAuthority
{
    Task<DriverAssignmentMaintenanceOutcome> AuthorizeAsync(string verifiedActorSubject,
        DriverAssignmentMaintenanceAction action, CancellationToken cancellationToken = default);
}

public sealed class DenyAllDriverAssignmentMaintenanceAuthority : IDriverAssignmentMaintenanceAuthority
{
    public Task<DriverAssignmentMaintenanceOutcome> AuthorizeAsync(string verifiedActorSubject,
        DriverAssignmentMaintenanceAction action, CancellationToken cancellationToken = default) =>
        Task.FromResult(DriverAssignmentMaintenanceOutcome.Denied);
}
