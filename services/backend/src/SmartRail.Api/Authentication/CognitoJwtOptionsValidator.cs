using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SmartRail.Api.Authentication;

/// <summary>
/// Rejects incomplete or unsafe Cognito validation configuration at host startup.
/// </summary>
public sealed class CognitoJwtOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<CognitoJwtOptions>
{
    public ValidateOptionsResult Validate(string? name, CognitoJwtOptions options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuerUri))
        {
            failures.Add("Authentication:Cognito:Issuer must be an absolute HTTPS issuer URL.");
        }
        else if (issuerUri.Scheme != Uri.UriSchemeHttps && !IsAllowedLocalHttpIssuer(issuerUri))
        {
            failures.Add(
                "Authentication:Cognito:Issuer must use HTTPS; HTTP is allowed only for a loopback issuer in Development or Testing.");
        }
        else if (!string.IsNullOrEmpty(issuerUri.Query) || !string.IsNullOrEmpty(issuerUri.Fragment))
        {
            failures.Add("Authentication:Cognito:Issuer must not include a query string or fragment.");
        }

        if (options.AllowedClientIds is null || options.AllowedClientIds.Length == 0)
        {
            failures.Add("Authentication:Cognito:AllowedClientIds must contain at least one human app-client ID.");
        }
        else
        {
            if (options.AllowedClientIds.Any(string.IsNullOrWhiteSpace))
            {
                failures.Add("Authentication:Cognito:AllowedClientIds cannot contain empty values.");
            }

            if (options.AllowedClientIds
                .Where(static clientId => !string.IsNullOrWhiteSpace(clientId))
                .Distinct(StringComparer.Ordinal)
                .Count() != options.AllowedClientIds.Count(static clientId => !string.IsNullOrWhiteSpace(clientId)))
            {
                failures.Add("Authentication:Cognito:AllowedClientIds cannot contain duplicates.");
            }
        }

        if (options.ExpectedAudience is not null && string.IsNullOrWhiteSpace(options.ExpectedAudience))
        {
            failures.Add(
                "Authentication:Cognito:ExpectedAudience must be omitted when no resource audience is configured; it cannot be empty.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private bool IsAllowedLocalHttpIssuer(Uri issuerUri) =>
        issuerUri.Scheme == Uri.UriSchemeHttp &&
        issuerUri.IsLoopback &&
        (environment.IsDevelopment() || environment.IsEnvironment("Testing"));
}
