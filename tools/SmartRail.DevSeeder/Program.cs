using Microsoft.EntityFrameworkCore;
using SmartRail.Infrastructure;

namespace SmartRail.DevSeeder;

internal static class SeedEntryPoint
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.FirstOrDefault() == "--cognito-test-roles")
                return await CognitoTestRoleBootstrap.RunCliAsync(args);
            var allowed = new[] { "--manifest", "--check", "--confirm-local-development", "--enable-development-sample-categories", "--station-display-t030" };
            if (args.Distinct(StringComparer.Ordinal).Count() != args.Length || args.Any(arg => !allowed.Contains(arg, StringComparer.Ordinal)) ||
                (args.Contains("--manifest") || args.Contains("--check")) && args.Length != 1)
                throw new SeedRefusalException("Use --manifest, --check, or both explicit seed confirmations as separate modes.");
            if (args.Length == 1 && args[0] == "--manifest")
            {
                Print(SeedManifest.Export());
                return 0;
            }
            if (args.Contains("--station-display-t030", StringComparer.Ordinal))
            {
                if (args.Length != 2 || !args.Contains("--confirm-local-development", StringComparer.Ordinal))
                    throw new SeedRefusalException("The T030 sample requires --station-display-t030 and --confirm-local-development only.");
                SeedSafety.ValidateEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                await using var t030Configured = new SmartRailDbContextFactory().CreateDbContext([]);
                var t030Connection = t030Configured.Database.GetConnectionString()!;
                SeedSafety.ValidateTarget(t030Connection);
                await using var t030Services = SeedComposition.Create(t030Connection);
                Print(await new StationDisplaySeedRunner(t030Services).RunAsync());
                return 0;
            }
            SeedSafety.ValidateEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            await using var configured = new SmartRailDbContextFactory().CreateDbContext([]);
            var connection = configured.Database.GetConnectionString()!;
            SeedSafety.ValidateTarget(connection);
            if (args.Contains("--check"))
            {
                Console.WriteLine("Validated Development target: 127.0.0.1:5432/smartrail_dev. No database connection or write attempted.");
                return 0;
            }
            if (!args.Contains("--confirm-local-development") || !args.Contains("--enable-development-sample-categories"))
                throw new SeedRefusalException("Both --confirm-local-development and --enable-development-sample-categories are required. No seed was attempted.");
            await using var services = SeedComposition.Create(connection);
            Print(await new DevelopmentSeedRunner(services).RunAsync());
            return 0;
        }
        catch (SeedRefusalException refusal)
        {
            // Only fixed, tool-owned messages are safe to show. Never display provider exception text.
            Console.Error.WriteLine("Development seed stopped safely. " + refusal.Message);
            return 1;
        }
        catch
        {
            Console.Error.WriteLine("Development seed stopped safely. Check configuration, applied migrations and owner/authorization availability. No connection details are printed. Previously committed steps are preserved and resumable.");
            return 1;
        }
    }

    private static void Print(object value) => Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(value,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
}
