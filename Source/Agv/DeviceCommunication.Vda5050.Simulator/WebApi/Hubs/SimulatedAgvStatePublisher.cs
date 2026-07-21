using Gebhardt.Acc.Common.WebApi.Hubs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;

/// <inheritdoc cref="ISimulatedAgvStatePublisher"/>
public class SimulatedAgvStatePublisher(IHubBroadcaster<SimulatedAgvStateHub, ISimulatedAgvStateClient> hubBroadcaster) : ISimulatedAgvStatePublisher
{
    /// <inheritdoc/>
    public Task SendStateChangedAsync(Vda5050StateDto state)
    {
        hubBroadcaster.Broadcast(client => client.Vda5050StateChanged(state));
        return Task.CompletedTask;
    }
}