using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;

/// <summary>
/// Publishes simulated agv VDA5050 state to all clients.
/// </summary>
public interface ISimulatedAgvStatePublisher
{
    /// <summary>
    /// Publishes the latest VDA5050 state of a simulated agv to all clients.
    /// </summary>
    /// <param name="state">The latest VDA5050 state.</param>
    Task SendStateChangedAsync(Vda5050StateDto state);
}