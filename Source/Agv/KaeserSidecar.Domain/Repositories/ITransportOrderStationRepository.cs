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
    /// <param name="stationIds">The Ids of the stations.</param>
    /// <returns>Collection of stations.</returns>
    Task<ICollection<Station>> GetStationsByIdsAsync(List<Guid> stationIds);
}
