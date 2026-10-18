namespace SmartRail.Api.Authentication;

/// <summary>
/// Configuration required to validate SmartRail human Cognito access tokens.
/// Token validation is configured by a later implementation phase.
/// </summary>
public sealed class CognitoJwtOptions
{
    public const string SectionName = "Authentication:Cognito";

    /// <summary>
    /// Exact trusted Cognito issuer URL. An isolated loopback HTTP issuer is
    /// accepted only in Development or Testing environments for local tests.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Human Cognito app-client identifiers allowed to authenticate to SmartRail.
    /// </summary>
    public string[] AllowedClientIds { get; set; } = [];

    /// <summary>
    /// Optional API resource audience. When omitted, only tokens without an
    /// aud claim may be accepted by the eventual token validator.
    /// </summary>
    public string? ExpectedAudience { get; set; }
}
