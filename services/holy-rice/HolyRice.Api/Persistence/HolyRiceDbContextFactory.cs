using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HolyRice.Api.Persistence;

public sealed class HolyRiceDbContextFactory : IDesignTimeDbContextFactory<HolyRiceDbContext>
{
    public HolyRiceDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<HolyRiceDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("HOLY_RICE_DB") ?? "Host=localhost;Port=5434;Database=holy_rice;Username=holy_rice;Password=local-development-only").Options);
}
