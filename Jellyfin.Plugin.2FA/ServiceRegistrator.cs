using Jellyfin.Plugin.TwoFA.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TwoFA;

/// <summary>
/// Registers plugin services with Jellyfin's DI container.
/// </summary>
public sealed class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<ITwoFactorUserStore, TwoFactorUserStore>();
        serviceCollection.AddSingleton<ITotpService, TotpService>();
    }
}
