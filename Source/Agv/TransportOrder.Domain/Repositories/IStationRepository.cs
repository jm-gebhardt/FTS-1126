using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for station-related requests.
/// </summary>
public interface IStationRepository
{
    /// <summary>
    /// Add all stations for an agv graph.
    /// </summary>
    /// <param name="agvGraph">The agv graph.</param>
    /// <returns></returns>
    Task AddStations(AgvGraph agvGraph);

    /// <summary>
    /// Get a single station by Id.
    /// </summary>
    /// <param name="id">The id of the station.</param>
    /// <returns>The station with the given id.</returns>
    Task<Station> GetStation(Guid id);

    /// <summary>
    /// Get a single station by name.
    /// </summary>
    /// <param name="name">The name of the station.</param>
    /// <returns>The station with the given name.</returns>
    Task<Station> GetStation(string name);

    /// <summary>
    /// Gets the base information of the station (w/o agvs and node state)
    /// </summary>
    /// <param name="name">The name of the station.</param>
    /// <returns>The station base information with the given name or null.</returns>
    Task<Station?> GetRequiredStationBaseInformation(string name);

    /// <summary>
    /// Get all stations.
    /// </summary>
    /// <param name="mapName">Optional map name.</param>
    /// <returns>All stations (optionally on the given map).</returns>
    Task<ICollection<Station>> GetStations(string? mapName=null);

    /// <summary>
    /// Locks a station.
    /// </summary>
    /// <param name="id">The station to lock.</param>
    /// <param name="user">The user that locked the station.</param>
    /// <param name="reason">The reason that the node was locked.</param>
    /// <param name="lockDescription">The description of the lock.</param>
    /// <param name="lockedOrderHandling">How orders concerning the station should be handled.</param>
    Task LockStation(Guid id, string user, string reason, string lockDescription, LockedOrderHandlingType lockedOrderHandling);

    /// <summary>
    /// Unlocks a station.
    /// </summary>
    /// <param name="id">The station to unlock.</param>
    Task UnlockStation(Guid id);
}