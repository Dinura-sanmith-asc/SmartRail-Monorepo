using Microsoft.EntityFrameworkCore;

namespace HolyRice.Api.Persistence;

public static class DevelopmentSeed
{
    public static async Task SeedAsync(HolyRiceDbContext db, IConfiguration config)
    {
        if (!await db.Categories.AnyAsync())
        {
            var rice = new MenuCategory { Id = Guid.Parse("d1415212-99cf-4acc-891e-54abb8f90b01"), Name = "Rice meals" };
            var snacks = new MenuCategory { Id = Guid.Parse("d1415212-99cf-4acc-891e-54abb8f90b02"), Name = "Snacks and drinks" };
            db.Categories.AddRange(rice, snacks);
            db.MenuItems.AddRange(
                new MenuItem { Id = Guid.Parse("d1415212-99cf-4acc-891e-54abb8f90b11"), Category = rice, Name = "Chicken rice", Description = "Rice with spiced chicken and vegetables", Price = 950, Available = true },
                new MenuItem { Id = Guid.Parse("d1415212-99cf-4acc-891e-54abb8f90b12"), Category = rice, Name = "Vegetable rice", Description = "Rice with seasonal vegetables", Price = 700, Available = true },
                new MenuItem { Id = Guid.Parse("d1415212-99cf-4acc-891e-54abb8f90b13"), Category = snacks, Name = "Tea", Description = "Freshly brewed Ceylon tea", Price = 250, Available = true },
                new MenuItem { Id = Guid.Parse("d1415212-99cf-4acc-891e-54abb8f90b14"), Category = snacks, Name = "Coconut roti", Description = "Temporarily unavailable", Price = 350, Available = false });
        }
        // These identifiers must be supplied from the SmartRail local station fixture.
        var stationConfig = config["HOLY_RICE_DEMO_STATIONS"];
        if (!string.IsNullOrWhiteSpace(stationConfig))
        {
            foreach (var entry in stationConfig.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = entry.Split('|', 2);
                if (parts.Length != 2 || !Guid.TryParse(parts[0], out var id) || id == Guid.Empty) continue;
                if (!await db.CollectionPoints.AnyAsync(x => x.StationId == id))
                    db.CollectionPoints.Add(new CollectionPoint { StationId = id, StationName = parts[1],
                        Instructions = "Collect at the Holy-Rice counter in the station concourse.", Available = true });
            }
        }
        await db.SaveChangesAsync();
    }
}
