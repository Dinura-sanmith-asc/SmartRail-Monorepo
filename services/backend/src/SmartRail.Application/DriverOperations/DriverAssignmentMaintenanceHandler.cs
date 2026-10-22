using SmartRail.Domain.DriverOperations;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Domain.DriverOperations.Entities;

namespace SmartRail.Application.DriverOperations;

public sealed record GrantDriverAssignmentCommand(string? VerifiedActorSubject, Guid AssignmentId,
    string DriverSubject, Guid JourneyInstanceId, string EvidenceReference);
public sealed record RevokeDriverAssignmentCommand(string? VerifiedActorSubject, Guid AssignmentId, string EvidenceReference);

/// <summary>Restricted internal command. Ordinary Controller grants never authorize assignment maintenance.</summary>
public sealed class DriverAssignmentMaintenanceHandler(IDriverOperationsRepository repository,
    IDriverAssignmentMaintenanceAuthority authority, IJourneyReadRepository journeys, TimeProvider timeProvider)
{
    public async Task<DriverResult<Guid>> GrantAsync(GrantDriverAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.VerifiedActorSubject)) return new(DriverOutcome.Forbidden);
        try
        {
            var decision = await authority.AuthorizeAsync(command.VerifiedActorSubject, DriverAssignmentMaintenanceAction.Grant, cancellationToken);
            if (decision != DriverAssignmentMaintenanceOutcome.Allowed) return new(decision == DriverAssignmentMaintenanceOutcome.Unavailable
                ? DriverOutcome.Unavailable : DriverOutcome.Forbidden);
            var now = timeProvider.GetUtcNow();
            var assignment = new DriverJourneyAssignment(command.AssignmentId, command.DriverSubject, command.JourneyInstanceId,
                now, command.VerifiedActorSubject, command.EvidenceReference);
            var owner = await journeys.ReadSnapshotAsync(command.JourneyInstanceId, cancellationToken);
            if (owner.Outcome == JourneyReadOutcome.NotFound) return new(DriverOutcome.NotFound);
            if (owner.Outcome != JourneyReadOutcome.Found) return new(DriverOutcome.Unavailable);
            var existing = await repository.FindAssignmentAsync(command.AssignmentId, cancellationToken);
            if (existing is not null) return existing.IsActive && existing.DriverSubject == command.DriverSubject &&
                existing.JourneyInstanceId == command.JourneyInstanceId && existing.SourceReference == command.EvidenceReference
                ? new(DriverOutcome.Success, existing.AssignmentId) : new(DriverOutcome.Conflict);
            if (await repository.FindActiveAssignmentAsync(command.DriverSubject, command.JourneyInstanceId, cancellationToken) is not null)
                return new(DriverOutcome.Conflict);
            var audit = new DriverAssignmentAudit(Guid.NewGuid(), assignment.AssignmentId, DriverAssignmentAuditAction.Granted,
                command.VerifiedActorSubject, now, command.EvidenceReference);
            await repository.GrantAssignmentAsync(assignment, audit, cancellationToken);
            return new(DriverOutcome.Created, assignment.AssignmentId);
        }
        catch (DriverAssignmentWriteConflictException) { return new(DriverOutcome.Conflict); }
        catch (ArgumentException) { return new(DriverOutcome.Invalid); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(DriverOutcome.Unavailable); }
    }
    public async Task<DriverResult<Guid>> RevokeAsync(RevokeDriverAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.VerifiedActorSubject)) return new(DriverOutcome.Forbidden);
        try
        {
            var decision = await authority.AuthorizeAsync(command.VerifiedActorSubject, DriverAssignmentMaintenanceAction.Revoke, cancellationToken);
            if (decision != DriverAssignmentMaintenanceOutcome.Allowed) return new(decision == DriverAssignmentMaintenanceOutcome.Unavailable
                ? DriverOutcome.Unavailable : DriverOutcome.Forbidden);
            if (command.AssignmentId == Guid.Empty || string.IsNullOrWhiteSpace(command.EvidenceReference)) return new(DriverOutcome.Invalid);
            var existing = await repository.FindAssignmentAsync(command.AssignmentId, cancellationToken);
            if (existing is null) return new(DriverOutcome.NotFound);
            if (!existing.IsActive) return new(DriverOutcome.Success, existing.AssignmentId);
            var revoked = await repository.RevokeAssignmentAsync(command.AssignmentId, command.VerifiedActorSubject,
                timeProvider.GetUtcNow(), Guid.NewGuid(), command.EvidenceReference, cancellationToken);
            return new(revoked ? DriverOutcome.Success : DriverOutcome.Conflict, command.AssignmentId);
        }
        catch (DriverAssignmentWriteConflictException) { return new(DriverOutcome.Conflict); }
        catch (ArgumentException) { return new(DriverOutcome.Invalid); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(DriverOutcome.Unavailable); }
    }
}
