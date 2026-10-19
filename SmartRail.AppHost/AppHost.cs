using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("SmartRail.AppHost is for local Development only; the local authentication issuer must never be started in another environment.");
}

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

builder.Build().Run();
