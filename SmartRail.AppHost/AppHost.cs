
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "SmartRail.AppHost is for local Development only.");
}

// Select authentication mode through an environment variable.
// Default to the existing local test issuer.
var authMode = Environment.GetEnvironmentVariable(
    "SMARTRAIL_AUTH_MODE") ?? "local";

if (authMode.Equals("aws", StringComparison.OrdinalIgnoreCase))
{
    // Real AWS Cognito authentication.
    // The API reads its Cognito configuration from
    // User Secrets or environment configuration.
    builder.AddProject<Projects.SmartRail_Api>(
            "smartrail-api",
            launchProfileName: "aspire")
        .WithEnvironment("DOTNET_ENVIRONMENT", Environments.Development)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environments.Development)
        .WithHttpHealthCheck("/health");
}
else if (authMode.Equals("local", StringComparison.OrdinalIgnoreCase))
{
    // Existing local authentication for development testing.
    var issuer = builder.AddProject<Projects.SmartRail_LocalAuthIssuer>(
            "local-auth-issuer",
            launchProfileName: "aspire")
        .WithEnvironment("DOTNET_ENVIRONMENT", Environments.Development)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environments.Development)
        .WithHttpHealthCheck("/health");

    var issuerUrl = issuer.GetEndpoint("http");

    issuer.WithEnvironment("LocalAuth__Issuer", issuerUrl);

    builder.AddProject<Projects.SmartRail_Api>(
            "smartrail-api",
            launchProfileName: "aspire")
        .WithEnvironment("DOTNET_ENVIRONMENT", Environments.Development)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environments.Development)
        .WithEnvironment("Authentication__Cognito__Issuer", issuerUrl)
        .WithHttpHealthCheck("/health")
        .WaitFor(issuer);
}
else
{
    throw new InvalidOperationException(
        "SMARTRAIL_AUTH_MODE must be 'local' or 'aws'.");
}

builder.Build().Run();
