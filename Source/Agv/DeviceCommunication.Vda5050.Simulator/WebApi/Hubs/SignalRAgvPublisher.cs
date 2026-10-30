using DeviceCommunication.Vda5050.Simulator.Configuration;
using DeviceCommunication.Vda5050.Simulator.Domain;
using DeviceCommunication.Vda5050.Simulator.Runtime;
using DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;

/// <summary>
/// Forwards everything to the inner publisher and additionally pushes state changes to the SignalR hub.
/// </summary>
public sealed class SignalRAgvPublisher(
    IAgvPublisher inner,
    IHubContext<SimulatedAgvStateHub, ISimulatedAgvStateClient> hubContext,
    IOptions<MqttOptions> mqttOptions,
    ILogger<SignalRAgvPublisher> logger) : IAgvPublisher
{
    public void PublishConnection(AgvIdentity id, ConnectionState state, uint headerId, DateTimeOffset timestamp)
        => inner.PublishConnection(id, state, headerId, timestamp);

    public void PublishState(AgvIdentity id, AgvStateSnapshot snapshot)
    {
        inner.PublishState(id, snapshot);
        _ = SendAsync(id, snapshot);
    }

    public void PublishFactsheet(AgvIdentity id, uint headerId, DateTimeOffset timestamp, string factsheetJson)
        => inner.PublishFactsheet(id, headerId, timestamp, factsheetJson);

    private async Task SendAsync(AgvIdentity id, AgvStateSnapshot snapshot)
    {
        try
        {
            var dto = Vda5050StateDtoMapper.Map(id, snapshot, mqttOptions.Value.ProtocolVersion);
                            await hubContext.Clients.All.Vda5050StateChanged(dto);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send state of AGV {AgvId} to SignalR clients", id);
        }
    }
}
