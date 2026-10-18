using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using SmartRail.Api.Authentication;
using SmartRail.Api.Authorization;
using SmartRail.Api.Contracts.Identity;
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
var cognitoOptions = builder.Configuration.GetSection(CognitoJwtOptions.SectionName).Get<CognitoJwtOptions>() ?? new CognitoJwtOptions();
builder.Services.AddCognitoJwtBearer(cognitoOptions, builder.Environment);
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddScoped<IAuthorizationHandler, SmartRailAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, SmartRailAuthorizationMiddlewareResultHandler>();

builder.Services.AddSmartRailInfrastructure(builder.Configuration);
builder.Services.AddSmartRailProblemDetails();

const string ReactDevCors = "ReactDevelopment";

builder.Services.AddCors(options =>
{
    options.AddPolicy(ReactDevCors, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
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

app.MapRailwayNetworkEndpoints();
app.MapIdentityEndpoints();


app.Run();

public partial class Program { }
