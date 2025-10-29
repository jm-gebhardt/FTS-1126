using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Repositories;

/// <summary>
/// Provides a repository for station-related queries.
/// </summary>
public interface IStationRepository : IRepository<Station>
{
    /// <summary>
    /// Add or update stations of a group.
    /// </summary>
    /// <param name="stations">All new stations of a group.</param>
    /// <param name="groupId">The id of the group.</param>
    Task AddOrUpdateStationsOfGroup(ICollection<Station> stations, Guid groupId);

    /// <summary>
    /// Get a station by the agv occupying it.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <returns>A station, if any.</returns>
    Task<Station?> GetStationByOccupyingAgv(Guid agvCorrelationId);

    /// <summary>
    /// Get a station by its id.
    /// </summary>
    /// <param name="stationId">The id of the station.</param>
    /// <returns>A station, if any.</returns>
    Task<Station?> GetStationById(Guid stationId);

    /// <summary>
    /// Get a station by its node id.
    /// </summary>
    /// <param name="nodeId">The node id of the station.</param>
    /// <returns>A station, if any.</returns>
    Task<Station?> GetStationByNodeId(Guid nodeId);

    /// <summary>
    /// Get station group by source and target.
    /// </summary>
    /// <param name="source">The source of the order.</param>
    /// <param name="target">The target of the order.</param>
    /// <returns>The station group, if any.</returns>
    Task<StationGroup?> GetStationGroup(string source, string target);
}