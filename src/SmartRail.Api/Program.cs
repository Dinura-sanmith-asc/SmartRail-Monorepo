using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using SmartRail.Api.Authentication;
using SmartRail.Api.Authorization;
using SmartRail.Api.Contracts.Identity;
using SmartRail.Api.Contracts.Incidents;
using SmartRail.Api.Endpoints;
using SmartRail.Api.Endpoints.JourneysStatus;
using SmartRail.Api.Endpoints.Incidents;
using SmartRail.Api.Errors;
using SmartRail.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Database command logs can contain query structure and operational details. Keep those out of API logs;
// application-level safe error mapping remains responsible for reporting unavailable operations.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);

// WebApplication.CreateBuilder loads appsettings, User Secrets in Development, and environment variables.
builder.Services.AddSingleton<IValidateOptions<CognitoJwtOptions>, CognitoJwtOptionsValidator>();
builder.Services
    .AddOptions<CognitoJwtOptions>()
    .Bind(builder.Configuration.GetSection(CognitoJwtOptions.SectionName))
    .ValidateOnStart();
var cognitoOptions = builder.Configuration.GetSection(CognitoJwtOptions.SectionName).Get<CognitoJwtOptions>() ?? new CognitoJwtOptions();
builder.Services.AddCognitoJwtBearer(cognitoOptions, builder.Environment);
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddScoped<IAuthorizationHandler, SmartRailAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, SmartRailAuthorizationMiddlewareResultHandler>();

builder.Services.AddSmartRailInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddSmartRailProblemDetails();

const string ReactDevCors = "ReactDevelopment";

builder.Services.AddCors(options =>
{
    options.AddPolicy(ReactDevCors, policy =>
    {
        policy
            .WithOrigins("http://localhost:5174")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        var components = document.Components ?? new OpenApiComponents();
        document.Components = components;
        var securitySchemes = components.SecuritySchemes ?? new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes = securitySchemes;
        securitySchemes["CognitoAccessToken"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Cognito access token; issuer, RSA signature, lifetime, token_use, client_id and configured resource-audience policy are validated. JWT groups do not grant SmartRail roles."
        };
        return Task.CompletedTask;
    });
    options.AddSchemaTransformer((schema, context, _) =>
    {
        if (context.JsonTypeInfo.Type == typeof(CurrentIdentityResponse))
        {
            schema.AdditionalPropertiesAllowed = false;
            if (schema.Properties?.TryGetValue("subject", out var subjectSchema) == true && subjectSchema is OpenApiSchema subject)
                subject.MinLength = 1;
            if (schema.Properties?.TryGetValue("roles", out var rolesSchema) == true && rolesSchema is OpenApiSchema roles)
            {
                roles.UniqueItems = true;
                if (roles.Items is OpenApiSchema roleLabels)
                    roleLabels.Enum = new List<JsonNode>
                    {
                        JsonValue.Create("Passenger")!, JsonValue.Create("Driver")!,
                        JsonValue.Create("Station Staff")!, JsonValue.Create("Controller")!
                    };
            }
        }
        else if (context.JsonTypeInfo.Type == typeof(IncidentResponse))
        {
            schema.AdditionalPropertiesAllowed = false;
            if (schema.Properties?.TryGetValue("state", out var stateSchema) == true && stateSchema is OpenApiSchema state)
                state.Enum = new List<JsonNode>
                {
                    JsonValue.Create("Reported")!, JsonValue.Create("In Progress")!,
                    JsonValue.Create("Resolved")!, JsonValue.Create("Closed")!
                };
            if (schema.Properties?.TryGetValue("source", out var sourceSchema) == true && sourceSchema is OpenApiSchema source)
                source.Enum = new List<JsonNode> { JsonValue.Create("IncidentManagement")! };
        }
        else if (context.JsonTypeInfo.Type == typeof(IncidentAssociationInput))
        {
            schema.AdditionalPropertiesAllowed = false;
            if (schema.Properties?.TryGetValue("kind", out var kindSchema) == true && kindSchema is OpenApiSchema kind)
                kind.Enum = new List<JsonNode>
                {
                    JsonValue.Create("Station")!, JsonValue.Create("Service")!,
                    JsonValue.Create("Journey")!, JsonValue.Create("Disruption")!
                };
        }
        else if (context.JsonTypeInfo.Type == typeof(CreateIncidentRequest))
        {
            schema.AdditionalPropertiesAllowed = false;
            if (schema.Properties?.TryGetValue("categoryCode", out var categorySchema) == true && categorySchema is OpenApiSchema category)
                category.MinLength = 1;
            if (schema.Properties?.TryGetValue("description", out var descriptionSchema) == true && descriptionSchema is OpenApiSchema description)
                description.MinLength = 1;
            if (schema.Properties?.TryGetValue("associations", out var associationsSchema) == true && associationsSchema is OpenApiSchema associations)
                associations.MinItems = 1;
        }
        else if (context.JsonTypeInfo.Type == typeof(UpdateIncidentRequest))
        {
            schema.AdditionalPropertiesAllowed = false;
            if (schema.Properties?.TryGetValue("expectedRevision", out var expectedRevisionSchema) == true && expectedRevisionSchema is OpenApiSchema expectedRevision)
                expectedRevision.Minimum = "1";
            if (schema.Properties?.TryGetValue("state", out var updateStateSchema) == true && updateStateSchema is OpenApiSchema updateState)
                updateState.Enum = new List<JsonNode>
                {
                    JsonValue.Create("Reported")!, JsonValue.Create("In Progress")!,
                    JsonValue.Create("Resolved")!, JsonValue.Create("Closed")!
                };
        }
        return Task.CompletedTask;
    });
});

var app = builder.Build();
app.UseSmartRailProblemDetails();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

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

if (app.Environment.IsDevelopment())
{
    app.UseCors(ReactDevCors);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapRailwayNetworkEndpoints();
app.MapIdentityEndpoints();
app.MapJourneysStatusReadEndpoints();
app.MapJourneysStatusPublicationEndpoints();
app.MapIncidentEndpoints();


app.Run();

public partial class Program { }
