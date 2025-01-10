using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides a consumer for <see cref="MqttAgvLastWill"/>
/// </summary>
[UsedImplicitly]
public class MqttLastWillConsumer(ILogger<MqttLastWillConsumer> logger) : IConsumer<MqttAgvLastWill>
{
    /// <summary>
    /// Handles the <see cref="MqttAgvLastWill"/> message
    /// </summary>
    public async Task Consume(ConsumeContext<MqttAgvLastWill> context)
    {
        ( _,  _,  _, string serialNumber, _, var connectionState) = context.Message;

        var agvCorrelationId = serialNumber.ToGuid();
        logger.LogInformation("Last will received {AgvSerialNumber}/{AgvCorrelationId} with connection state {ConnectionState}", serialNumber, agvCorrelationId, connectionState);
        
        if (connectionState is ConnectionState.Offline or ConnectionState.ConnectionBroken)
        {
            await context.Publish(new AgvLastWillReceived(agvCorrelationId));
        }
    }
}