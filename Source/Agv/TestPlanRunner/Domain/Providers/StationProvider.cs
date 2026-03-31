using System.Collections.Immutable;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Providers;

/// <inheritdoc/>
public class StationProvider: IStationProvider
{
    private volatile ImmutableHashSet<string> _stations = ImmutableHashSet<string>.Empty;

    /// <inheritdoc/>
    public bool StationExists(string station)
    {
       return _stations.Contains(station);
    }

    /// <inheritdoc/>
    public void Update(ICollection<string> stations)
    {
        Interlocked.Exchange(ref _stations, stations.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase));
    }
}