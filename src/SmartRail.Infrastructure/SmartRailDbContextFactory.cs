using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure;

public sealed class SmartRailDbContextFactory : IDesignTimeDbContextFactory<SmartRailDbContext>
{
    public SmartRailDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SmartRailDbContext");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__SmartRailDbContext to a local PostgreSQL connection string before running EF tooling.");
        }

        var options = new DbContextOptionsBuilder<SmartRailDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SmartRailDbContext(options);
    }
}
