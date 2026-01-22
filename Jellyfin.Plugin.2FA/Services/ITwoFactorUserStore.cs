using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TwoFA.Models;

namespace Jellyfin.Plugin.TwoFA.Services;

/// <summary>
/// Abstraction for storing per-user 2FA settings.
/// </summary>
public interface ITwoFactorUserStore
{
    /// <summary>
    /// Gets the settings for a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The settings for the user.</returns>
    Task<TwoFactorUserSettings> GetAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all stored user settings.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All user settings.</returns>
    Task<IReadOnlyDictionary<Guid, TwoFactorUserSettings>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves settings for a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="settings">The user settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(Guid userId, TwoFactorUserSettings settings, CancellationToken cancellationToken);
}
