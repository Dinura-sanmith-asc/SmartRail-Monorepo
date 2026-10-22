using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace SmartRail.Api.Authentication;

/// <summary>Validates Cognito-specific access-token claims after cryptographic JWT validation.</summary>
public sealed class CognitoAccessTokenValidator(IOptions<CognitoJwtOptions> options)
{
    public bool IsValid(ClaimsPrincipal? principal, SecurityToken? securityToken)
    {
        if (principal?.Identity?.IsAuthenticated != true || securityToken is null) return false;
        var subject = SingleClaim(principal, "sub");
        var tokenUse = SingleClaim(principal, "token_use");
        var clientId = SingleClaim(principal, "client_id");
        if (string.IsNullOrWhiteSpace(subject) || !string.Equals(tokenUse, "access", StringComparison.Ordinal) ||
            clientId is null || !options.Value.AllowedClientIds.Contains(clientId, StringComparer.Ordinal))
            return false;

        var audiences = securityToken switch
        {
            JwtSecurityToken jwt => jwt.Audiences.ToArray(),
            JsonWebToken jsonWebToken => jsonWebToken.Audiences.ToArray(),
            _ => []
        };

        var expectedAudience = options.Value.ExpectedAudience;
        if (string.IsNullOrWhiteSpace(expectedAudience))
            return audiences.Length == 0;

        return audiences.Length > 0 && audiences.Contains(expectedAudience, StringComparer.Ordinal);
    }

    private static string? SingleClaim(ClaimsPrincipal principal, string claimType)
    {
        var claims = principal.FindAll(claimType).Select(claim => claim.Value).ToArray();
        return claims.Length == 1 ? claims[0] : null;
    }
}
