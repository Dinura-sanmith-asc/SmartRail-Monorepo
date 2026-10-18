using Microsoft.Extensions.Options;
using SmartRail.Api.Authentication;
using SmartRail.Api.Endpoints;
using SmartRail.Api.Errors;
using SmartRail.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// WebApplication.CreateBuilder loads appsettings, User Secrets in Development, and environment variables.
builder.Services.AddSingleton<IValidateOptions<CognitoJwtOptions>, CognitoJwtOptionsValidator>();
builder.Services
    .AddOptions<CognitoJwtOptions>()
    .Bind(builder.Configuration.GetSection(CognitoJwtOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSmartRailInfrastructure(builder.Configuration);
builder.Services.AddSmartRailProblemDetails();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseSmartRailProblemDetails();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "SmartRail API v1"
        );
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.MapRailwayNetworkEndpoints();

app.Run();

public partial class Program { }
