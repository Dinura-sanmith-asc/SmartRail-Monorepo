namespace SmartRail.IntegrationTests.JourneysStatus;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class JourneyPostgresCollection : ICollectionFixture<SmartRail.IntegrationTests.RailwayNetwork.PostgreSqlTestDatabaseFixture>
{
    public const string Name = "Feature 003 isolated PostgreSQL";
}
