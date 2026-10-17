using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartRailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(SmartRailDbContext.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{SmartRailDbContext.ConnectionStringName}' is required. " +
                "Set it with .NET User Secrets or the ConnectionStrings__SmartRailDbContext environment variable.");
        }

        services.AddDbContext<SmartRailDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
