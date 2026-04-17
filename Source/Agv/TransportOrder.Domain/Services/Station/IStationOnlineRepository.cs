namespace Gebhardt.Acc.TransportOrder.Domain.Services.Station;

/// <summary>
/// Provides a repository for the station online state.
/// </summary>
public interface IStationOnlineRepository
{
    /// <summary>
    /// Gets the current online state of a station.
    /// </summary>
    /// <param name="stationId">The station id.</param>
    /// <returns>True if online, null if not monitored.</returns>
    bool? Get(Guid stationId);

    /// <summary>
    /// Updates the online state of a station.
    /// </summary>
    /// <param name="stationId">The station id.</param>
    /// <param name="isOnline">The new online state.</param>
    void Update(Guid stationId, bool isOnline);
}