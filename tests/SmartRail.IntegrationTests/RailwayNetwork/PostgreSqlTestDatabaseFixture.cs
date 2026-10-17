using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace SmartRail.IntegrationTests.RailwayNetwork;

public sealed class PostgreSqlTestDatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"smartrail_it_{Guid.NewGuid():N}";
    private string? _adminConnectionString;
    private bool _databaseCreated;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable("SMART_RAIL_TEST_ADMIN_CONNECTION");
        if (string.IsNullOrWhiteSpace(_adminConnectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL integration tests require SMART_RAIL_TEST_ADMIN_CONNECTION for the dedicated local test host (Compose port 5433).");
        }

        var admin = new NpgsqlConnectionStringBuilder(_adminConnectionString);
        var adminHost = admin.Host;
        if (string.IsNullOrWhiteSpace(adminHost) ||
            !IsLoopbackHost(adminHost) ||
            admin.Port != 5433 ||
            !string.Equals(admin.Username, "smartrail_test_admin", StringComparison.Ordinal) ||
            !string.Equals(admin.Database, "postgres", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The integration-test admin connection must target the dedicated local Compose test host (127.0.0.1:5433), use smartrail_test_admin, and connect to the postgres maintenance database; never use RDS or the developer database.");
        }

        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        try
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", connection);
            await command.ExecuteNonQueryAsync();
            _databaseCreated = true;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Could not create an isolated PostgreSQL integration-test database. Verify the dedicated test container is healthy and the admin connection is valid.",
                exception);
        }

        admin.Database = _databaseName;
        admin.ApplicationName = "SmartRail integration tests";
        ConnectionString = admin.ConnectionString;
    }

    public WebApplicationFactory<Program> CreateWebApplicationFactory()
    {
        if (!_databaseCreated)
        {
            throw new InvalidOperationException("Initialize the PostgreSQL test database before creating the API test host.");
        }

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(
                $"ConnectionStrings:{SmartRail.Infrastructure.RailwayNetwork.SmartRailDbContext.ConnectionStringName}",
                ConnectionString);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{SmartRail.Infrastructure.RailwayNetwork.SmartRailDbContext.ConnectionStringName}"] = ConnectionString
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<SmartRail.Infrastructure.RailwayNetwork.SmartRailDbContext>>();
                services.RemoveAll<SmartRail.Infrastructure.RailwayNetwork.SmartRailDbContext>();
                services.AddDbContext<SmartRail.Infrastructure.RailwayNetwork.SmartRailDbContext>(options =>
                    options.UseNpgsql(ConnectionString));
            });
        });
    }

    public async Task DisposeAsync()
    {
        if (!_databaseCreated || _adminConnectionString is null)
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
        _databaseCreated = false;
    }

    private static bool IsLoopbackHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
}
