namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <summary>
/// A selector for the next target
/// </summary>
public interface ITargetSelector
{
    /// <summary>
    /// Select the next target.
    /// </summary>
    /// <param name="consideredStations">Number of considered stations.</param>
    /// <returns>The target station if any.</returns>
    Task<string?> Select(int consideredStations);
}