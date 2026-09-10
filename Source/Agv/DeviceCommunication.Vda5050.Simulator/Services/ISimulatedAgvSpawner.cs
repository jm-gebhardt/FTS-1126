using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services;

/// <summary>
/// Brings the fleet in line with the agv type configuration.
/// </summary>
public interface ISimulatedAgvSpawner
{
    /// <summary>
    /// Applies the new configuration from one agv type configuration to another.
    /// </summary>
    /// <param name="currentConfiguration">The configuration that is currently in effect, or null if there was none.</param>
    /// <param name="newConfiguration">The configuration that should be in effect, or null if it was deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ApplyAsync(SimulatedAgvTypeConfiguration? currentConfiguration, SimulatedAgvTypeConfiguration? newConfiguration, CancellationToken cancellationToken = default);
}