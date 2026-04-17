using Gebhardt.Acc.StationCommunication.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.StationCommunication.Domain.Repositories;

/// <summary>
/// Tracks per-channel TCP connection state and derives per-station online status.
/// </summary>
[PublicAPI]
public interface IPlcConnectionRepository
{
    /// <summary>
    /// Initializes (or re-initializes) the repository with station-to-channel mappings.
    /// Signals <see cref="WaitForInitializationAsync"/> to complete on the first call.
    /// </summary>
    /// <param name="stations">Stations to register.</param>
    /// <param name="activeChannelKeys">Channel keys that are enabled.</param>
    void Initialize(ICollection<Station> stations, IEnumerable<string> activeChannelKeys);

    /// <summary>
    /// Waits until <see cref="Initialize"/> has been called at least once.
    /// Used by PLC communication managers to delay connecting until station mappings are available.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    Task WaitForInitializationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the current online state of all monitored stations.
    /// </summary>
    IEnumerable<(Guid stationId, bool isOnline)> GetAllStationStates();

    /// <summary>
    /// Updates a channel's connection state and returns stations whose derived online status changed.
    /// </summary>
    /// <param name="channelKey">The channel key, e.g.: FROMWESTATION.</param>
    /// <param name="isConnected">Whether the channel is now connected.</param>
    /// <param name="includeUntracked">When true, yield a station even if it has no prior explicit state./>.
    /// </param>
    /// <returns>Stations whose IsOnline state has changed (or that were never tracked when <paramref name="includeUntracked"/> is set).</returns>
    IEnumerable<(Guid stationId, bool isOnline)> UpdateChannelState(string channelKey, bool isConnected, bool includeUntracked = false);
}