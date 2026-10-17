using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.UnitTests.RailwayNetwork;

public sealed class StationAuditEntitiesTests
{
    private static readonly DateTimeOffset Started = new(2026, 10, 17, 8, 0, 0, TimeSpan.FromHours(5.5));

    [Fact]
    public void Import_run_preserves_source_and_actor_evidence_and_counts()
    {
        var runId = Guid.NewGuid();
        var run = new StationImportRun(runId, "source-v2", "sha256:abc", "submitter-1", "approver-2",
            Started, Started.AddMinutes(3), 2, 1, 4);

        Assert.Equal(runId, run.ImportRunId);
        Assert.Equal("source-v2", run.SourceRevision);
        Assert.Equal("sha256:abc", run.ContentHash);
        Assert.Equal("submitter-1", run.SubmittedBy);
        Assert.Equal("approver-2", run.ApprovedBy);
        Assert.Equal((2, 1, 4), (run.CreatedCount, run.UpdatedCount, run.UnchangedCount));
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Import_run_rejects_negative_counts(int created, int updated, int unchanged)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewRun(createdCount: created, updatedCount: updated, unchangedCount: unchanged));
    }

    [Fact]
    public void Import_run_rejects_invalid_identity_evidence_and_timestamp_order()
    {
        Assert.Throws<ArgumentException>(() => NewRun(importRunId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => NewRun(sourceRevision: " "));
        Assert.Throws<ArgumentException>(() => NewRun(contentHash: " "));
        Assert.Throws<ArgumentException>(() => NewRun(submittedBy: " "));
        Assert.Throws<ArgumentException>(() => NewRun(approvedBy: " "));
        Assert.Throws<ArgumentException>(() => NewRun(completedAt: Started.AddTicks(-1)));
    }

    [Fact]
    public void Station_change_records_only_minimal_action_and_changed_fields()
    {
        var change = NewChange();

        Assert.Equal(StationChangeAction.Updated, change.Action);
        Assert.Equal(new[] { "name", "district" }, change.ChangedFieldNames);
        Assert.Equal("railway-maintainer-test", change.ChangedBy);
        Assert.DoesNotContain(typeof(StationChange).GetProperties(), property => property.Name.Contains("Snapshot", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Station_change_requires_stable_change_run_and_station_ids(int emptyId)
    {
        var changeId = emptyId == 0 ? Guid.Empty : Guid.NewGuid();
        var runId = emptyId == 1 ? Guid.Empty : Guid.NewGuid();
        var stationId = emptyId == 2 ? Guid.Empty : Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new StationChange(changeId, runId, stationId,
            StationChangeAction.Created, ["name"], Started, "actor-1"));
    }

    [Fact]
    public void Station_change_rejects_empty_or_duplicate_field_evidence_and_unknown_action()
    {
        Assert.Throws<ArgumentException>(() => NewChange(fields: []));
        Assert.Throws<ArgumentException>(() => NewChange(fields: ["name", "name"]));
        Assert.Throws<ArgumentException>(() => NewChange(fields: [" "]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StationChange(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            (StationChangeAction)99, ["name"], Started, "actor-1"));
        Assert.Throws<ArgumentException>(() => NewChange(changedBy: " "));
    }

    private static StationImportRun NewRun(
        Guid? importRunId = null,
        string sourceRevision = "source-v1",
        string contentHash = "sha256:abc",
        string submittedBy = "submitter-1",
        string approvedBy = "approver-1",
        DateTimeOffset? completedAt = null,
        int createdCount = 1,
        int updatedCount = 0,
        int unchangedCount = 0) =>
        new(importRunId ?? Guid.NewGuid(), sourceRevision, contentHash, submittedBy, approvedBy, Started,
            completedAt ?? Started.AddMinutes(1), createdCount, updatedCount, unchangedCount);

    private static StationChange NewChange(
        IReadOnlyList<string>? fields = null,
        string changedBy = "railway-maintainer-test") =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StationChangeAction.Updated,
            fields ?? ["name", "district"], Started, changedBy);
}