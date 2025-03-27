using Gebhardt.Acc.TransportOrder.Domain.Models.Station;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides an interface for extending the transport order station repository
/// </summary>
public interface ITransportOrderStationRepository : TransportOrder.Domain.Repositories.IStationRepository
{
    /// <summary>
    /// Get all stations that match the given names.
    /// </summary>
    /// <param name="stationNames">The names of the stations.</param>
    /// <returns>Collection of stations.</returns>
    Task<ICollection<Station>> GetStationsByNames(string[] stationNames);

    /// <summary>
    /// Get the station name that is at the location of the given node.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <returns>The station name,  if the node is co-located with a station</returns>
    Task<string?> GetStationAtLocationOfNode(string nodeId);
}
