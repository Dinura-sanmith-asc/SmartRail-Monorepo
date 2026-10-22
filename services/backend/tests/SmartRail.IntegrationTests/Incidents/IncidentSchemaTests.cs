using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartRail.Domain.Incidents;
using SmartRail.Domain.Incidents.Entities;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentSchemaTests(PostgreSqlTestDatabaseFixture database) : IAsyncLifetime
{
    private SmartRailDbContext _db = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(database.ConnectionString).Options;
        _db = new SmartRailDbContext(options);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Generated_migration_creates_incident_schema_and_keys()
    {
        var applied = await _db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, id => id.EndsWith("_IncidentManagement", StringComparison.Ordinal));
        var tables = await _db.Database.SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema='public'").ToArrayAsync();
        Assert.Contains("incident", tables);
        Assert.Contains("incident_revision", tables);
        Assert.Contains("incident_association", tables);
        Assert.Contains("incident_idempotency_record", tables);
        var indexes = await _db.Database.SqlQueryRaw<string>("SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname='public'").ToArrayAsync();
        Assert.Contains("ux_incident_association_active_resource", indexes);
        Assert.Contains("ux_incident_association_active_disruption", indexes);
        Assert.Contains("ux_incident_idempotency_scope_key", indexes);
    }

    [Fact]
    public async Task Revision_and_active_association_uniqueness_are_enforced()
    {
        var incident = IncidentTestData.CreateIncident();
        _db.Incidents.Add(incident);
        _db.IncidentRevisions.Add(IncidentTestData.CreateRevision(incident));
        await _db.SaveChangesAsync();

        var resource = Guid.NewGuid();
        _db.Stations.Add(new Station(resource, "F004-" + Guid.NewGuid().ToString("N")[..10], "Incident schema station"));
        _db.IncidentAssociations.Add(new IncidentAssociation(Guid.NewGuid(), incident.IncidentId,
            IncidentAssociationKind.Station, resource, IncidentTestData.Now, 1));
        await _db.SaveChangesAsync();
        _db.IncidentAssociations.Add(new IncidentAssociation(Guid.NewGuid(), incident.IncidentId,
            IncidentAssociationKind.Station, resource, IncidentTestData.Now, 1));
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        _db.ChangeTracker.Clear();

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var invalidRevision = new NpgsqlCommand("INSERT INTO incident_revision (incident_id, revision, state, actor_subject, source, occurred_at, association_evidence) VALUES (@id, 0, 'Reported', 'actor', 'IncidentManagement', now(), '[]'::jsonb)", connection);
        invalidRevision.Parameters.AddWithValue("id", incident.IncidentId);
        await Assert.ThrowsAsync<PostgresException>(() => invalidRevision.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task Check_constraints_and_restrictive_foreign_keys_are_enforced()
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var invalidIncident = new NpgsqlCommand("INSERT INTO incident (incident_id, category_code, description, state, revision, reporter_subject, reported_at, created_at, updated_at) VALUES (@id, 'bad', 'bad', 'Reported', 0, 'actor', now(), now(), now())", connection);
        invalidIncident.Parameters.AddWithValue("id", Guid.NewGuid());
        await Assert.ThrowsAsync<PostgresException>(() => invalidIncident.ExecuteNonQueryAsync());

        await using var invalidLink = new NpgsqlCommand("INSERT INTO incident_association (association_id, incident_id, kind, resource_id, added_at, added_revision) VALUES (@id, @missing, 'Station', @resource, now(), 1)", connection);
        invalidLink.Parameters.AddWithValue("id", Guid.NewGuid());
        invalidLink.Parameters.AddWithValue("missing", Guid.NewGuid());
        invalidLink.Parameters.AddWithValue("resource", Guid.NewGuid());
        await Assert.ThrowsAsync<PostgresException>(() => invalidLink.ExecuteNonQueryAsync());

        _db.ChangeTracker.Clear();
        var existingIncident = await _db.Incidents.AsNoTracking().FirstAsync();
        _db.IncidentAssociations.Add(new IncidentAssociation(Guid.NewGuid(), existingIncident.IncidentId,
            IncidentAssociationKind.Station, Guid.NewGuid(), DateTimeOffset.UtcNow, existingIncident.Revision));
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }
}
