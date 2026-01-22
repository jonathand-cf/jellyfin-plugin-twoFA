using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TwoFA.Classes;

/// <summary>
/// PluginPageInfo with a replacement flag for templated content.
/// </summary>
public sealed class ExtraPageInfo : PluginPageInfo
{
    /// <summary>
    /// Gets or sets a value indicating whether the file needs string replacement.
    /// </summary>
    public bool NeedsReplacement { get; set; }
}
