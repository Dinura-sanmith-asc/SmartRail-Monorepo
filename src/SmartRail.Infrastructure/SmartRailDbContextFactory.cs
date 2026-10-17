using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SmartRail.Infrastructure.RailwayNetwork;
using System.Net;
using System.Xml.Linq;

namespace SmartRail.Infrastructure;

public sealed class SmartRailDbContextFactory : IDesignTimeDbContextFactory<SmartRailDbContext>
{
    public SmartRailDbContext CreateDbContext(string[] args)
    {
        var apiProjectDirectory = FindApiProjectDirectory();
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(apiProjectDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true);

        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            var userSecretsId = ReadUserSecretsId(apiProjectDirectory);
            if (!string.IsNullOrWhiteSpace(userSecretsId))
            {
                Microsoft.Extensions.Configuration.UserSecretsConfigurationExtensions.AddUserSecrets(
                    configurationBuilder, userSecretsId, reloadOnChange: false);
            }
        }

        // Match ASP.NET Core precedence: environment variables override JSON and User Secrets.
        var configuration = configurationBuilder
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("SmartRailDbContext");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No connection string is configured for 'ConnectionStrings:SmartRailDbContext'. " +
                "In Development, add it with 'dotnet user-secrets set \"ConnectionStrings:SmartRailDbContext\" \"<local PostgreSQL connection string>\" --project src/SmartRail.Api', " +
                "or provide the ConnectionStrings__SmartRailDbContext environment variable. No database connection was attempted.");
        }

        EnsureLocalDatabase(connectionString);

        var options = new DbContextOptionsBuilder<SmartRailDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SmartRailDbContext(options);
    }

    private static string FindApiProjectDirectory()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                var apiDirectory = Path.Combine(directory.FullName, "src", "SmartRail.Api");
                if (File.Exists(Path.Combine(apiDirectory, "SmartRail.Api.csproj")))
                {
                    return apiDirectory;
                }

                directory = directory.Parent;
            }
        }

        throw new InvalidOperationException(
            "Could not locate src/SmartRail.Api/SmartRail.Api.csproj. Run EF tooling from the SmartRail repository root or a child directory.");
    }

    private static string? ReadUserSecretsId(string apiProjectDirectory)
    {
        var projectFile = Path.Combine(apiProjectDirectory, "SmartRail.Api.csproj");
        var project = XDocument.Load(projectFile);
        return project.Descendants("UserSecretsId").FirstOrDefault()?.Value.Trim();
    }

    private static void EnsureLocalDatabase(string connectionString)
    {
        NpgsqlConnectionStringBuilder parsed;
        try
        {
            parsed = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "The configured SmartRailDbContext connection string is invalid. Its value has been omitted for safety.");
        }

        var hosts = (parsed.Host ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (hosts.Length == 0 || hosts.Any(host => !IsLoopbackHost(host)))
        {
            throw new InvalidOperationException(
                "EF design-time configuration accepts local PostgreSQL hosts only, to prevent accidental production connections. Connection details have been omitted.");
        }
    }

    private static bool IsLoopbackHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
}
