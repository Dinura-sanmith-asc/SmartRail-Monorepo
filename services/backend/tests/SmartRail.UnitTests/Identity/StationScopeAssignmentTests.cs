using SmartRail.Domain.Identity.Entities;

namespace SmartRail.UnitTests.Identity;

public sealed class StationScopeAssignmentTests
{
    private static readonly DateTimeOffset GrantedAt = new(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Creates_scope_for_existing_station_identity_and_preserves_subject()
    {
        var stationId = Guid.NewGuid();
        var assignment = NewAssignment(stationId: stationId, subject: "Cognito:Staff/Case-Sensitive");

        Assert.True(assignment.IsActive);
        Assert.Equal(stationId, assignment.StationId);
        Assert.Equal("Cognito:Staff/Case-Sensitive", assignment.CognitoSubject);
        Assert.Equal("Cognito:Approver", assignment.GrantedBySubject);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Requires_assignment_station_and_actor_identity(int emptyValue)
    {
        var id = emptyValue == 0 ? Guid.Empty : Guid.NewGuid();
        var stationId = emptyValue == 1 ? Guid.Empty : Guid.NewGuid();
        var subject = emptyValue == 2 ? " " : "subject-1";
        var actor = emptyValue == 3 ? " " : "actor-1";

        Assert.Throws<ArgumentException>(() => new StationScopeAssignment(id, subject, stationId, GrantedAt, actor));
    }

    [Fact]
    public void Requires_non_default_UTC_grant_timestamp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewAssignment(grantedAt: DateTimeOffset.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => NewAssignment(
            grantedAt: new DateTimeOffset(2026, 10, 18, 13, 30, 0, TimeSpan.FromHours(5.5))));
    }

    [Fact]
    public void Revokes_once_and_does_not_allow_reactivation()
    {
        var assignment = NewAssignment();
        var revokedAt = GrantedAt.AddMinutes(30);

        Assert.Throws<ArgumentOutOfRangeException>(() => assignment.Revoke("actor-2", GrantedAt.AddTicks(-1)));
        assignment.Revoke("Cognito:Revoker", revokedAt);

        Assert.False(assignment.IsActive);
        Assert.Equal("Cognito:Revoker", assignment.RevokedBySubject);
        Assert.Equal(revokedAt, assignment.RevokedAtUtc);
        Assert.Throws<InvalidOperationException>(() => assignment.Revoke("actor-3", revokedAt.AddMinutes(1)));
    }

    [Fact]
    public void Regrant_uses_a_new_scope_lifecycle()
    {
        var original = NewAssignment();
        original.Revoke("revoker", GrantedAt.AddHours(1));
        var regrant = NewAssignment(id: Guid.NewGuid(), stationId: original.StationId, grantedAt: GrantedAt.AddHours(2));

        Assert.NotEqual(original.Id, regrant.Id);
        Assert.Equal(original.StationId, regrant.StationId);
        Assert.False(original.IsActive);
        Assert.True(regrant.IsActive);
    }

    [Fact]
    public void Scope_audits_preserve_assignment_actor_time_and_optional_evidence()
    {
        var assignment = NewAssignment();
        var grantAudit = StationScopeAssignmentAudit.ForGrant(Guid.NewGuid(), assignment, "station-scope-ticket");

        Assert.Equal(assignment.Id, grantAudit.StationScopeAssignmentId);
        Assert.Equal(AssignmentAuditAction.Granted, grantAudit.Action);
        Assert.Equal("Cognito:Approver", grantAudit.ActorSubject);
        Assert.Equal(GrantedAt, grantAudit.OccurredAtUtc);
        Assert.Equal("station-scope-ticket", grantAudit.EvidenceReference);

        assignment.Revoke("Cognito:Revoker", GrantedAt.AddHours(1));
        var revokeAudit = StationScopeAssignmentAudit.ForRevocation(Guid.NewGuid(), assignment);
        Assert.Equal(AssignmentAuditAction.Revoked, revokeAudit.Action);
        Assert.Equal("Cognito:Revoker", revokeAudit.ActorSubject);
        Assert.Equal(GrantedAt.AddHours(1), revokeAudit.OccurredAtUtc);
        Assert.Throws<ArgumentException>(() => StationScopeAssignmentAudit.ForRevocation(Guid.NewGuid(), NewAssignment()));
    }

    [Fact]
    public void Rejects_invalid_or_mutable_station_scope_audit_evidence()
    {
        var assignment = NewAssignment();
        Assert.Throws<ArgumentException>(() => new StationScopeAssignmentAudit(Guid.Empty, assignment.Id,
            AssignmentAuditAction.Granted, "actor", GrantedAt));
        Assert.Throws<ArgumentException>(() => new StationScopeAssignmentAudit(Guid.NewGuid(), Guid.Empty,
            AssignmentAuditAction.Granted, "actor", GrantedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StationScopeAssignmentAudit(Guid.NewGuid(), assignment.Id,
            (AssignmentAuditAction)99, "actor", GrantedAt));
        Assert.Throws<ArgumentException>(() => new StationScopeAssignmentAudit(Guid.NewGuid(), assignment.Id,
            AssignmentAuditAction.Granted, " ", GrantedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StationScopeAssignmentAudit(Guid.NewGuid(), assignment.Id,
            AssignmentAuditAction.Granted, "actor", default));
        Assert.Throws<ArgumentException>(() => new StationScopeAssignmentAudit(Guid.NewGuid(), assignment.Id,
            AssignmentAuditAction.Granted, "actor", GrantedAt, " "));
        Assert.All(typeof(StationScopeAssignmentAudit).GetProperties(), property => Assert.Null(property.SetMethod));
    }

    private static StationScopeAssignment NewAssignment(
        Guid? id = null,
        string subject = "subject-1",
        Guid? stationId = null,
        DateTimeOffset? grantedAt = null,
        string grantedBy = "Cognito:Approver") =>
        new(id ?? Guid.NewGuid(), subject, stationId ?? Guid.NewGuid(), grantedAt ?? GrantedAt, grantedBy);
}
