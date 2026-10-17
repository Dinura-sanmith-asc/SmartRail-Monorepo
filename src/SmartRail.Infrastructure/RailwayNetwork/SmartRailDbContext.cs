using Microsoft.EntityFrameworkCore;

namespace SmartRail.Infrastructure.RailwayNetwork;

public sealed class SmartRailDbContext(DbContextOptions<SmartRailDbContext> options) : DbContext(options)
{
    public const string ConnectionStringName = "SmartRailDbContext";
}
