namespace EntraIdPoc.Api.Configuration;

/// <summary>
/// Strongly-typed configuration for Microsoft Entra ID.
/// Maps to appsettings.json section "AzureAd".
/// </summary>
public class EntraConfig
{
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string Domain { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty; // For confidential client (API)
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Full authority URL for token validation.
    /// </summary>
    public string Authority => $"{Instance}{TenantId}/v2.0";

    /// <summary>
    /// JWKS endpoint for signature validation.
    /// </summary>
    public string JwksUri => $"{Instance}{TenantId}/discovery/v2.0/keys";
}
