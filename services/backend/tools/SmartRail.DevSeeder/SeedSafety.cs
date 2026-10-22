using Npgsql;

namespace SmartRail.DevSeeder;

internal sealed class SeedRefusalException(string message) : InvalidOperationException(message);

internal static class SeedSafety
{
    internal static void ValidateEnvironment(string? dotnetEnvironment, string? aspnetEnvironment)
    {
        if (dotnetEnvironment != "Development" ||
            aspnetEnvironment is not (null or "Development"))
            throw new SeedRefusalException("Explicit Development environment is required.");
    }

    internal static void ValidateTarget(string connection)
    {
        NpgsqlConnectionStringBuilder parsed;
        try { parsed = new NpgsqlConnectionStringBuilder(connection); }
        catch (ArgumentException) { throw new SeedRefusalException("Connection-string syntax is invalid; its value is omitted."); }
        var mismatches = new List<string>();
        if (parsed.Host != "127.0.0.1") mismatches.Add("Host must be exactly 127.0.0.1");
        if (parsed.Port != 5432) mismatches.Add("Port must be 5432");
        if (parsed.Database != "smartrail_dev") mismatches.Add("Database must be smartrail_dev");
        if (parsed.Username != "smartrail_dev") mismatches.Add("Username must be smartrail_dev");
        if (!string.IsNullOrEmpty(parsed.Options) || !string.IsNullOrEmpty(parsed.SearchPath)) mismatches.Add("Options and SearchPath overrides must be absent");
        if (mismatches.Count != 0)
            throw new SeedRefusalException("Local target rejected: " + string.Join("; ", mismatches) + ". Configured values are omitted.");
    }
}
