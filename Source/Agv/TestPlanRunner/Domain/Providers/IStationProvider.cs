using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Models.Stations;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Providers;

/// <summary>
/// Provides available stations.
/// </summary>
public interface IStationProvider
{
    /// <summary>
    /// True, if the station/lane exists.
    /// </summary>
    /// <param name="station">The station (name or lane).</param>
    bool StationExists(string station);

    /// <summary>
    /// True, if the station exists and has one of the given types.
    /// </summary>
    /// <param name="station">The station name.</param>
    /// <param name="types">Accepted station types.</param>
    bool StationHasAnyType(string station, params AgvGraph.NodeType[] types);

    /// <summary>
    /// Updates stations.
    /// </summary>
    /// <param name="stations">The new station and lane names.</param>
    void Update(ICollection<string> stations);

    /// <summary>
    /// Updates the typed stations.
    /// </summary>
    /// <param name="stations">The new stations.</param>
    void UpdateStationTypes(ICollection<StationTypeInfo> stations);
}