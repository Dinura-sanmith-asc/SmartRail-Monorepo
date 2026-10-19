using SmartRail.IntegrationTests.RailwayNetwork;
using SmartRail.IntegrationTests.Identity;

namespace SmartRail.IntegrationTests.Incidents;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IncidentPostgresCollection : ICollectionFixture<PostgreSqlTestDatabaseFixture>, ICollectionFixture<LocalOidcFixture>
{
    public const string Name = "Feature 004 isolated PostgreSQL";
}
