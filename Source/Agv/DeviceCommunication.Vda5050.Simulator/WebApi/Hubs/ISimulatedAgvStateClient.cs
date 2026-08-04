using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;

/// <summary>
/// Provides client methods for simulated agv state events.
/// </summary>
[SignalRHub(SimulatedAgvStateHub.UrlPath, documentNames: [HubApiGroup.GroupName])]
public interface ISimulatedAgvStateClient
{
    /// <summary>
    /// The VDA5050 state of a simulated agv changed.
    /// </summary>
    /// <param name="state">The latest VDA5050 state.</param>
    [SignalRMethod(nameof(Vda5050StateChanged))]
    Task Vda5050StateChanged(Vda5050StateDto state);
}