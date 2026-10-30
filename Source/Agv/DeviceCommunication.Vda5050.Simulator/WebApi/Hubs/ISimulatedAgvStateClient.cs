using DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using SignalRSwaggerGen.Attributes;

namespace DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;

/// <summary>
/// Provides client methods for simulated agv state events.
/// </summary>
[SignalRHub(SimulatedAgvStateHub.UrlPath, documentNames: [HubApiGroup.GroupName])]
public interface ISimulatedAgvStateClient
{
    /// <summary>
    /// The state of a simulated agv changed.
    /// </summary>
    /// <param name="state">The latest state.</param>
    [SignalRMethod(nameof(Vda5050StateChanged))]
    Task Vda5050StateChanged(Vda5050StateDto state);
}
