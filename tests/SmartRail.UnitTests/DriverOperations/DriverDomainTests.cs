using SmartRail.Application.DriverOperations;
using SmartRail.Domain.DriverOperations;
using SmartRail.Domain.DriverOperations.Entities;

namespace SmartRail.UnitTests.DriverOperations;

public sealed class DriverDomainTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
    private static DriverJourneyAssignment Assignment(string subject = "opaque driver") => new(Guid.NewGuid(), subject, Guid.NewGuid(), At, "trusted operator", "approved manifest");

    [Fact]
    public void AssignmentPreservesOpaqueSubjectAndHasOneWayRevocation()
    {
        var assignment = Assignment("  opaque subject  ");
        Assert.Equal("  opaque subject  ", assignment.DriverSubject);
        Assert.True(assignment.IsActive);
        assignment.Revoke("trusted revoker", At.AddMinutes(1));
        Assert.False(assignment.IsActive);
        Assert.Equal("trusted revoker", assignment.RevokedBySubject);
        Assert.Throws<InvalidOperationException>(() => assignment.Revoke("trusted revoker", At.AddMinutes(2)));
        var regrant = Assignment(assignment.DriverSubject);
        Assert.True(regrant.IsActive);
        Assert.NotEqual(assignment.AssignmentId, regrant.AssignmentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AssignmentRejectsMissingDriver(string subject) => Assert.Throws<ArgumentException>(() => Assignment(subject));

    [Fact]
    public void AssignmentRejectsMissingKeysActorProvenanceAndNonUtcTime()
    {
        Assert.Throws<ArgumentException>(() => new DriverJourneyAssignment(Guid.Empty, "driver", Guid.NewGuid(), At, "actor", "source"));
        Assert.Throws<ArgumentException>(() => new DriverJourneyAssignment(Guid.NewGuid(), "driver", Guid.Empty, At, "actor", "source"));
        Assert.Throws<ArgumentException>(() => new DriverJourneyAssignment(Guid.NewGuid(), "driver", Guid.NewGuid(), At, " ", "source"));
        Assert.Throws<ArgumentException>(() => new DriverJourneyAssignment(Guid.NewGuid(), "driver", Guid.NewGuid(), At, "actor", " "));
        Assert.Throws<ArgumentException>(() => new DriverJourneyAssignment(Guid.NewGuid(), "driver", Guid.NewGuid(), At.ToOffset(TimeSpan.FromHours(1)), "actor", "source"));
        Assert.Throws<ArgumentException>(() => Assignment().Revoke("actor", At.AddMinutes(-1)));
        Assert.Throws<ArgumentException>(() => Assignment().Revoke(" ", At));
    }

    [Fact]
    public void AuditRequiresRecognizedActionAndImmutableEvidence()
    {
        var id = Guid.NewGuid();
        var grant = new DriverAssignmentAudit(Guid.NewGuid(), id, DriverAssignmentAuditAction.Granted, "actor", At, " source ");
        Assert.Equal(id, grant.AssignmentId);
        Assert.Equal("source", grant.EvidenceReference);
        var revoke = new DriverAssignmentAudit(Guid.NewGuid(), id, DriverAssignmentAuditAction.Revoked, "actor", At.AddMinutes(1), "revocation");
        Assert.Equal(DriverAssignmentAuditAction.Revoked, revoke.Action);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DriverAssignmentAudit(Guid.NewGuid(), id, (DriverAssignmentAuditAction)99, "actor", At, "source"));
        Assert.Throws<ArgumentException>(() => new DriverAssignmentAudit(Guid.NewGuid(), id, DriverAssignmentAuditAction.Granted, "actor", At, " "));
        Assert.Throws<ArgumentException>(() => new DriverAssignmentAudit(Guid.Empty, id, DriverAssignmentAuditAction.Granted, "actor", At, "source"));
        Assert.Throws<ArgumentException>(() => new DriverAssignmentAudit(Guid.NewGuid(), Guid.Empty, DriverAssignmentAuditAction.Granted, "actor", At, "source"));
        Assert.Throws<ArgumentException>(() => new DriverAssignmentAudit(Guid.NewGuid(), id, DriverAssignmentAuditAction.Granted, "actor", default, "source"));
    }

    [Fact]
    public void ObservationIsAuthoredImmutableEvidenceNotOfficialState()
    {
        var assignment = Assignment();
        var request = Guid.NewGuid();
        var observation = new DriverObservation(Guid.NewGuid(), assignment.AssignmentId, assignment.JourneyInstanceId, assignment.DriverSubject, request, "  Departed station.  ", At, 3);
        Assert.Equal("Departed station.", observation.Text);
        Assert.Equal(assignment.DriverSubject, observation.DriverSubject);
        Assert.Equal(assignment.JourneyInstanceId, observation.JourneyInstanceId);
        Assert.Equal(request, observation.RequestId);
        Assert.Equal(3, observation.JourneyRevision);
        Assert.DoesNotContain(typeof(DriverObservation).GetProperties(), p => p.SetMethod?.IsPublic == true);
        Assert.DoesNotContain(typeof(DriverObservation).GetProperties(), p => p.Name == "Status");
        Assert.DoesNotContain(typeof(DriverAssignmentAudit).GetProperties(), p => p.SetMethod?.IsPublic == true);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ObservationRejectsEmptyText(string text) => Assert.Throws<ArgumentException>(() => Observation(text: text));

    [Fact]
    public void ObservationValidatesBoundsIdentityRevisionAndTime()
    {
        Assert.Equal(4000, Observation(text: new string('a', 4000)).Text.Length);
        Assert.Throws<ArgumentException>(() => Observation(text: new string('a', 4001)));
        Assert.Throws<ArgumentException>(() => Observation(subject: " "));
        Assert.Throws<ArgumentException>(() => Observation(subject: new string('a', 301)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Observation(revision: 0));
        Assert.Throws<ArgumentException>(() => Observation(at: At.ToOffset(TimeSpan.FromHours(1))));
        Assert.Throws<ArgumentException>(() => new DriverObservation(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "driver", Guid.NewGuid(), "text", At, 1));
        Assert.Throws<ArgumentException>(() => new DriverObservation(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "driver", Guid.NewGuid(), "text", At, 1));
        Assert.Throws<ArgumentException>(() => new DriverObservation(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "driver", Guid.NewGuid(), "text", At, 1));
        Assert.Throws<ArgumentException>(() => new DriverObservation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "driver", Guid.Empty, "text", At, 1));
    }

    [Theory]
    [InlineData(DriverAssignmentMaintenanceAction.Grant)]
    [InlineData(DriverAssignmentMaintenanceAction.Revoke)]
    public async Task ProductionMaintenanceAuthorityAlwaysDenies(DriverAssignmentMaintenanceAction action) =>
        Assert.Equal(DriverAssignmentMaintenanceOutcome.Denied, await new DenyAllDriverAssignmentMaintenanceAuthority().AuthorizeAsync("even-controller", action));

    private static DriverObservation Observation(string text = "Progress observed", string subject = "driver", int revision = 1, DateTimeOffset? at = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), subject, Guid.NewGuid(), text, at ?? At, revision);
}
