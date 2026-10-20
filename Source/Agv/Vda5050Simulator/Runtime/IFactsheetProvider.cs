using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Provides the factsheet json of an agv type.
/// </summary>
public interface IFactsheetProvider
{
    /// <summary>
    /// Gets the factsheet json of the agv type or null if none is available.
    /// </summary>
    Task<string?> GetAsync(AgvIdentity agvIdentity, CancellationToken cancellationToken);
}
