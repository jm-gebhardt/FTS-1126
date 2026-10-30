using DeviceCommunication.Vda5050.Simulator.Domain;

namespace DeviceCommunication.Vda5050.Simulator.Runtime;

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
