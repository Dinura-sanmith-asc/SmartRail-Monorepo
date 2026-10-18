using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SmartRail.Api.Authentication;

public static class CognitoJwtBearerExtensions
{
    public static IServiceCollection AddCognitoJwtBearer(
        this IServiceCollection services,
        CognitoJwtOptions cognito,
        IHostEnvironment environment)
    {
        var hasIssuerUri = Uri.TryCreate(cognito.Issuer, UriKind.Absolute, out var issuer);
        services.AddSingleton<CognitoAccessTokenValidator>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (hasIssuerUri)
                {
                    options.Authority = cognito.Issuer.TrimEnd('/');
                    options.MetadataAddress = $"{options.Authority}/.well-known/openid-configuration";
                }
                options.RequireHttpsMetadata = !hasIssuerUri || issuer!.Scheme == Uri.UriSchemeHttps ||
                    !(issuer!.IsLoopback && (environment.IsDevelopment() || environment.IsEnvironment("Testing")));
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.RefreshOnIssuerKeyNotFound = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = cognito.Issuer,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateAudience = false,
                    RequireAudience = false,
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var validator = context.HttpContext.RequestServices.GetRequiredService<CognitoAccessTokenValidator>();
                        if (!validator.IsValid(context.Principal, context.SecurityToken))
                            context.Fail("Invalid access token.");
                        return Task.CompletedTask;
                    }
                };
            });
        return services;
    }
}
