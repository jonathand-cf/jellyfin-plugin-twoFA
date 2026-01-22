using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TwoFA.Models;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.TwoFA.Services;

/// <summary>
/// File-backed 2FA user settings store.
/// </summary>
public sealed class TwoFactorUserStore : ITwoFactorUserStore, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _filePath;
    private Dictionary<Guid, TwoFactorUserSettings>? _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorUserStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    public TwoFactorUserStore(IApplicationPaths applicationPaths)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);

        string pluginDir = Path.Combine(applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.2FA");
        _filePath = Path.Combine(pluginDir, "users.json");
    }

    /// <inheritdoc />
    public async Task<TwoFactorUserSettings> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var map = await LoadAsync(cancellationToken).ConfigureAwait(false);
        return map.TryGetValue(userId, out var settings) ? settings : new TwoFactorUserSettings();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, TwoFactorUserSettings>> GetAllAsync(CancellationToken cancellationToken)
    {
        var map = await LoadAsync(cancellationToken).ConfigureAwait(false);
        return new Dictionary<Guid, TwoFactorUserSettings>(map);
    }

    /// <inheritdoc />
    public async Task SetAsync(Guid userId, TwoFactorUserSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var map = await LoadInternalAsync(cancellationToken).ConfigureAwait(false);
            map[userId] = settings;
            await SaveInternalAsync(map, cancellationToken).ConfigureAwait(false);
            _cache = map;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<Dictionary<Guid, TwoFactorUserSettings>> LoadAsync(CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<Dictionary<Guid, TwoFactorUserSettings>> LoadInternalAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_filePath))
        {
            _cache = new Dictionary<Guid, TwoFactorUserSettings>();
            return _cache;
        }

        using var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var map = await JsonSerializer.DeserializeAsync<Dictionary<Guid, TwoFactorUserSettings>>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        _cache = map ?? new Dictionary<Guid, TwoFactorUserSettings>();
        return _cache;
    }

    private async Task SaveInternalAsync(Dictionary<Guid, TwoFactorUserSettings> map, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, map, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mutex.Dispose();
    }
}
