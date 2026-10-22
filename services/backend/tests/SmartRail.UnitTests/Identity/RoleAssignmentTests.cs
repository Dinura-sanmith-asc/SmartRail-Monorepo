using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;

namespace SmartRail.UnitTests.Identity;

public sealed class RoleAssignmentTests
{
    private static readonly DateTimeOffset GrantedAt = new(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Creates_active_assignment_and_preserves_opaque_subject_exactly()
    {
        const string subject = "Cognito:Subject/Case-Sensitive";
        const string actor = "Cognito:Maintainer/Actor";
        var assignment = NewAssignment(subject: subject, grantedBy: actor);

        Assert.True(assignment.IsActive);
        Assert.Equal(subject, assignment.CognitoSubject);
        Assert.Equal(actor, assignment.GrantedBySubject);
        Assert.Equal(RoleCode.Passenger, assignment.RoleCode);
        Assert.Null(assignment.RevokedAtUtc);
        Assert.Null(assignment.RevokedBySubject);
    }

    [Fact]
    public void Rejects_public_or_unknown_assignable_role_codes()
    {
        Assert.DoesNotContain("Public", Enum.GetNames<RoleCode>());
        Assert.Throws<ArgumentOutOfRangeException>(() => NewAssignment(roleCode: (RoleCode)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => NewAssignment(roleCode: (RoleCode)99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Requires_stable_assignment_identity(int emptyId)
    {
        var id = emptyId == 0 ? Guid.Empty : Guid.NewGuid();
        var subject = emptyId == 1 ? " " : "subject-1";
        var actor = emptyId == 2 ? " " : "actor-1";

        Assert.Throws<ArgumentException>(() => new RoleAssignment(id, subject, RoleCode.Driver, GrantedAt, actor));
    }

    [Fact]
    public void Requires_non_default_UTC_grant_timestamp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewAssignment(grantedAt: DateTimeOffset.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => NewAssignment(
            grantedAt: new DateTimeOffset(2026, 10, 18, 13, 30, 0, TimeSpan.FromHours(5.5))));
    }

    [Fact]
    public void Revokes_once_with_actor_and_UTC_time_and_rejects_pre_grant_time()
    {
        var assignment = NewAssignment();
        var revokedAt = GrantedAt.AddHours(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => assignment.Revoke("actor-2", GrantedAt.AddTicks(-1)));
        assignment.Revoke("Cognito:Revoker", revokedAt);

        Assert.False(assignment.IsActive);
        Assert.Equal("Cognito:Revoker", assignment.RevokedBySubject);
        Assert.Equal(revokedAt, assignment.RevokedAtUtc);
        Assert.Throws<InvalidOperationException>(() => assignment.Revoke("actor-3", revokedAt.AddMinutes(1)));
        Assert.Throws<ArgumentException>(() => NewAssignment().Revoke(" ", revokedAt));
    }

    [Fact]
    public void Regrant_creates_a_new_lifecycle_and_preserves_old_revocation()
    {
        var original = NewAssignment();
        original.Revoke("revoker", GrantedAt.AddHours(1));
        var regrant = NewAssignment(id: Guid.NewGuid(), grantedAt: GrantedAt.AddHours(2));

        Assert.NotEqual(original.Id, regrant.Id);
        Assert.False(original.IsActive);
        Assert.True(regrant.IsActive);
        Assert.Equal(GrantedAt.AddHours(1), original.RevokedAtUtc);
    }

    [Fact]
    public void Grant_and_revocation_audits_preserve_assignment_actor_time_and_evidence()
    {
        var assignment = NewAssignment(grantedBy: "grant-actor");
        var grantAudit = RoleAssignmentAudit.ForGrant(Guid.NewGuid(), assignment, "change-ticket-42");

        Assert.Equal(assignment.Id, grantAudit.RoleAssignmentId);
        Assert.Equal(AssignmentAuditAction.Granted, grantAudit.Action);
        Assert.Equal("grant-actor", grantAudit.ActorSubject);
        Assert.Equal(GrantedAt, grantAudit.OccurredAtUtc);
        Assert.Equal("change-ticket-42", grantAudit.EvidenceReference);

        assignment.Revoke("revoke-actor", GrantedAt.AddHours(1));
        var revokeAudit = RoleAssignmentAudit.ForRevocation(Guid.NewGuid(), assignment);
        Assert.Equal(AssignmentAuditAction.Revoked, revokeAudit.Action);
        Assert.Equal("revoke-actor", revokeAudit.ActorSubject);
        Assert.Equal(GrantedAt.AddHours(1), revokeAudit.OccurredAtUtc);
        Assert.Null(revokeAudit.EvidenceReference);
        Assert.Throws<ArgumentException>(() => RoleAssignmentAudit.ForRevocation(Guid.NewGuid(), NewAssignment()));
    }

    [Fact]
    public void Rejects_invalid_or_mutable_role_audit_evidence()
    {
        var assignment = NewAssignment();
        Assert.Throws<ArgumentException>(() => new RoleAssignmentAudit(Guid.Empty, assignment.Id,
            AssignmentAuditAction.Granted, "actor", GrantedAt));
        Assert.Throws<ArgumentException>(() => new RoleAssignmentAudit(Guid.NewGuid(), Guid.Empty,
            AssignmentAuditAction.Granted, "actor", GrantedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoleAssignmentAudit(Guid.NewGuid(), assignment.Id,
            (AssignmentAuditAction)99, "actor", GrantedAt));
        Assert.Throws<ArgumentException>(() => new RoleAssignmentAudit(Guid.NewGuid(), assignment.Id,
            AssignmentAuditAction.Granted, " ", GrantedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoleAssignmentAudit(Guid.NewGuid(), assignment.Id,
            AssignmentAuditAction.Granted, "actor", default));
        Assert.Throws<ArgumentException>(() => new RoleAssignmentAudit(Guid.NewGuid(), assignment.Id,
            AssignmentAuditAction.Granted, "actor", GrantedAt, " "));
        Assert.All(typeof(RoleAssignmentAudit).GetProperties(), property => Assert.Null(property.SetMethod));
    }

    private static RoleAssignment NewAssignment(
        Guid? id = null,
        string subject = "subject-1",
        RoleCode roleCode = RoleCode.Passenger,
        DateTimeOffset? grantedAt = null,
        string grantedBy = "actor-1") =>
        new(id ?? Guid.NewGuid(), subject, roleCode, grantedAt ?? GrantedAt, grantedBy);
}
