using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TwoFA.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether TOTP is enabled globally.
    /// </summary>
    public bool EnableTotp { get; set; } = true;

    /// <summary>
    /// Gets or sets the issuer displayed in authenticator apps.
    /// </summary>
    public string TotpIssuer { get; set; } = "Jellyfin";

    /// <summary>
    /// Gets or sets a value indicating whether users can opt in to 2FA.
    /// </summary>
    public bool AllowUserEnrollment { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Authentik is enabled as an optional provider.
    /// </summary>
    public bool EnableAuthentik { get; set; }

    /// <summary>
    /// Gets or sets the Authentik base URL (e.g. https://auth.example.com).
    /// </summary>
    public string AuthentikBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Authentik client id.
    /// </summary>
    public string AuthentikClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Authentik client secret.
    /// </summary>
    public string AuthentikClientSecret { get; set; } = string.Empty;
}
