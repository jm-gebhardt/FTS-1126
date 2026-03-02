using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;

/// <summary>
/// Handles the <see cref="MqttConnection"/> message.
/// </summary>
[UsedImplicitly]
public class MqttConnectionConsumer(ILogger<MqttConnectionConsumer> logger) :
    IConsumer<MqttConnection>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<MqttConnection> context)
    {
        (_, _, _, string serialNumber, _, var connectionState) = context.Message;

        var agvCorrelationId = serialNumber.ToGuid();

        if (connectionState is MqttConnectionState.Online)
        {
            logger.LogInformation("Agv {AgvSerialNumber}/{AgvCorrelationId} went online, waiting for state ", serialNumber, agvCorrelationId);
            return;
        }

        if (connectionState is MqttConnectionState.Offline or MqttConnectionState.ConnectionBroken)
        {
            logger.LogInformation("Last will received {AgvSerialNumber}/{AgvCorrelationId} with connection state {ConnectionState}", serialNumber, agvCorrelationId, connectionState);
            await context.Publish(new AgvLastWillReceived(agvCorrelationId));
        }
    }
}