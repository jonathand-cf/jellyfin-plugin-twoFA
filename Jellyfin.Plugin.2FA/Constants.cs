using System;
using Jellyfin.Plugin.TwoFA.Classes;

namespace Jellyfin.Plugin.TwoFA;

/// <summary>
/// Constants for this plugin.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Gets the embedded login files for the 2FA SSO page.
    /// </summary>
    public static readonly ExtraPageInfo[] LoginFiles =
    [
        new()
        {
            Name = "index",
            EmbeddedResourcePath = $"{typeof(Plugin).Namespace}.Assets.Login.login.html",
            NeedsReplacement = true
        },
        new()
        {
            Name = "login.css",
            EmbeddedResourcePath = $"{typeof(Plugin).Namespace}.Assets.Login.login.css",
            NeedsReplacement = true
        },
        new()
        {
            Name = "login.js",
            EmbeddedResourcePath = $"{typeof(Plugin).Namespace}.Assets.Login.login.js",
            NeedsReplacement = true
        }
    ];

    /// <summary>
    /// Gets the plugin name used for auth sessions.
    /// </summary>
    public static string PluginName => "TwoFA";

    /// <summary>
    /// Gets the login route for the 2FA page.
    /// </summary>
    public static string LoginRoute => "/sso/2fa";
}
