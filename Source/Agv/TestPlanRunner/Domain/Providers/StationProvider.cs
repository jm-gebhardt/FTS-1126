using System.Collections.Immutable;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Models.Stations;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Providers;

/// <inheritdoc/>
public class StationProvider : IStationProvider
{
    private volatile ImmutableHashSet<string> _stations = ImmutableHashSet<string>.Empty;
    private volatile ImmutableDictionary<string, ImmutableHashSet<AgvGraph.NodeType>> _stationTypes = ImmutableDictionary<string, ImmutableHashSet<AgvGraph.NodeType>>.Empty;

    /// <inheritdoc/>
    public bool StationExists(string station)
    {
        return _stations.Contains(station);
    }

    /// <inheritdoc/>
    public bool StationHasAnyType(string station, params AgvGraph.NodeType[] types)
    {
        return _stationTypes.TryGetValue(station, out var stationTypes) && types.Any(stationTypes.Contains);
    }

    /// <inheritdoc/>
    public void Update(ICollection<string> stations)
    {
        Interlocked.Exchange(ref _stations, stations.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public void UpdateStationTypes(ICollection<StationTypeInfo> stations)
    {
        Interlocked.Exchange(ref _stationTypes, stations.ToImmutableDictionary(
            s => s.Name,
            s => s.AdditionalTypes.Append(s.PrimaryType).ToImmutableHashSet(),
            StringComparer.OrdinalIgnoreCase));
    }
}