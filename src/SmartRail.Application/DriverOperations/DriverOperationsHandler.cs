using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.JourneysStatus.Projections;
using SmartRail.Application.JourneysStatus.Queries;
using SmartRail.Domain.DriverOperations.Entities;
using SmartRail.Domain.Identity;

namespace SmartRail.Application.DriverOperations;

public enum DriverOutcome { Success, Created, Invalid, Unauthorized, Forbidden, NotFound, Conflict, Unavailable }
public sealed record DriverResult<T>(DriverOutcome Outcome, T? Value = default);
public sealed record DriverJourneyContext(JourneySnapshotView Snapshot, IReadOnlyList<JourneyUpdateView> Updates);
public sealed record DriverAssignmentView(Guid AssignmentId, string DriverSubject, DriverJourneyContext Journey);
public sealed record DriverObservationView(Guid ObservationId, Guid JourneyInstanceId, Guid AssignmentId,
    string DriverSubject, string Text, DateTimeOffset ReportedAtUtc, int JourneyRevision)
{
    public static DriverObservationView From(DriverObservation value) => new(value.ObservationId,
        value.JourneyInstanceId, value.AssignmentId, value.DriverSubject, value.Text, value.ReportedAtUtc, value.JourneyRevision);
}

/// <summary>Authoritative current roles and assignments are rechecked on every call, including retries.</summary>
public sealed class DriverOperationsHandler(IDriverOperationsRepository repository,
    ResolveCurrentRolesHandler roles, AuthorizeOperationHandler authorization,
    GetJourneyHandler journeys, GetJourneyUpdatesHandler updates, TimeProvider timeProvider)
{
    public async Task<DriverResult<IReadOnlyList<DriverAssignmentView>>> GetAssignmentsAsync(string? subject,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject)) return new(DriverOutcome.Unauthorized);
        try
        {
            var current = await roles.HandleAsync(new(new(subject)), cancellationToken);
            if (current.Outcome == RoleResolutionOutcome.Unavailable) return new(DriverOutcome.Unavailable);
            if (!RolePermissionCatalog.IsGranted(current.Roles, PermissionAction.ViewDriverAssignments)) return new(DriverOutcome.Forbidden);
            var controller = current.Roles.Contains(RoleCode.Controller);
            var assignments = await repository.GetActiveAssignmentsAsync(controller ? null : subject, cancellationToken);
            var views = new List<DriverAssignmentView>();
            foreach (var assignment in assignments)
            {
                var decision = await authorization.HandleAsync(new(new(subject), PermissionAction.ViewDriverAssignments,
                    ProtectedResource.Journey(assignment.JourneyInstanceId)), cancellationToken);
                if (decision.Outcome != AuthorizationOutcome.Allowed) return new(Map(decision.Outcome));
                var snapshot = await journeys.HandleAsync(new(assignment.JourneyInstanceId), cancellationToken);
                var history = await updates.HandleAsync(new(assignment.JourneyInstanceId), cancellationToken);
                if (snapshot.Outcome != JourneyReadOutcome.Found || snapshot.Value is null ||
                    history.Outcome != JourneyReadOutcome.Found || history.Value is null) return new(DriverOutcome.Unavailable);
                // Separate owner reads must describe one current published revision.
                if (history.Value.Any(item => item.Revision > snapshot.Value.Revision)) return new(DriverOutcome.Unavailable);
                views.Add(new(assignment.AssignmentId, assignment.DriverSubject, new(snapshot.Value, history.Value)));
            }
            return new(DriverOutcome.Success, views);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(DriverOutcome.Unavailable); }
    }

    public async Task<DriverResult<DriverObservationView>> SubmitObservationAsync(string? subject, Guid journeyId,
        Guid requestId, string? text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject)) return new(DriverOutcome.Unauthorized);
        if (journeyId == Guid.Empty || requestId == Guid.Empty || string.IsNullOrWhiteSpace(text) || text.Trim().Length > 4000)
            return new(DriverOutcome.Invalid);
        var canonicalText = text.Trim();
        try
        {
            var decision = await authorization.HandleAsync(new(new(subject), PermissionAction.SubmitDriverProgressObservation,
                ProtectedResource.Journey(journeyId)), cancellationToken);
            if (decision.Outcome != AuthorizationOutcome.Allowed) return new(Map(decision.Outcome));
            var assignment = await repository.FindActiveAssignmentAsync(subject, journeyId, cancellationToken);
            if (assignment is null) return new(DriverOutcome.Forbidden);
            var snapshot = await journeys.HandleAsync(new(journeyId), cancellationToken);
            if (snapshot.Outcome == JourneyReadOutcome.NotFound) return new(DriverOutcome.NotFound);
            if (snapshot.Outcome != JourneyReadOutcome.Found || snapshot.Value is null) return new(DriverOutcome.Unavailable);
            var replay = await repository.FindRequestAsync(subject, requestId, cancellationToken);
            if (replay is not null) return Replay(replay, journeyId, canonicalText);
            var observation = new DriverObservation(Guid.NewGuid(), assignment.AssignmentId, journeyId, subject,
                requestId, canonicalText, timeProvider.GetUtcNow(), snapshot.Value.Revision);
            try { await repository.AddObservationAsync(observation, cancellationToken); }
            catch (DriverAssignmentWriteConflictException) { return new(DriverOutcome.Forbidden); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch
            {
                // A concurrent identical insert may have won the unique subject/request key.
                var winner = await repository.FindRequestAsync(subject, requestId, cancellationToken);
                if (winner is null) return new(DriverOutcome.Unavailable);
                return Replay(winner, journeyId, canonicalText);
            }
            return new(DriverOutcome.Created, DriverObservationView.From(observation));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(DriverOutcome.Unavailable); }
    }

    public async Task<DriverResult<IReadOnlyList<DriverObservationView>>> GetObservationsAsync(string? subject,
        Guid journeyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject)) return new(DriverOutcome.Unauthorized);
        if (journeyId == Guid.Empty) return new(DriverOutcome.Invalid);
        try
        {
            var decision = await authorization.HandleAsync(new(new(subject), PermissionAction.ReviewDriverObservations,
                ProtectedResource.Journey(journeyId)), cancellationToken);
            if (decision.Outcome != AuthorizationOutcome.Allowed) return new(Map(decision.Outcome));
            var snapshot = await journeys.HandleAsync(new(journeyId), cancellationToken);
            if (snapshot.Outcome == JourneyReadOutcome.NotFound) return new(DriverOutcome.NotFound);
            if (snapshot.Outcome != JourneyReadOutcome.Found) return new(DriverOutcome.Unavailable);
            var records = await repository.GetObservationsAsync(journeyId, cancellationToken);
            return new(DriverOutcome.Success, records.OrderBy(x => x.ReportedAtUtc).ThenBy(x => x.ObservationId)
                .Select(DriverObservationView.From).ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(DriverOutcome.Unavailable); }
    }

    private static DriverResult<DriverObservationView> Replay(DriverObservation value, Guid journeyId, string text) =>
        value.JourneyInstanceId == journeyId && value.Text == text
            ? new(DriverOutcome.Success, DriverObservationView.From(value)) : new(DriverOutcome.Conflict);
    private static DriverOutcome Map(AuthorizationOutcome outcome) => outcome == AuthorizationOutcome.Unavailable
        ? DriverOutcome.Unavailable : DriverOutcome.Forbidden;
}
