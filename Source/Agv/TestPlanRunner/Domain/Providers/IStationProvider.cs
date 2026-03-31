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
    /// Updates stations.
    /// </summary>
    /// <param name="stations">The new station and lane names.</param>
    void Update(ICollection<string> stations);
}