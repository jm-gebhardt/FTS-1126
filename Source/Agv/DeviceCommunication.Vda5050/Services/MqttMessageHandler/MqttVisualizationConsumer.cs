using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;

/// <summary>
/// Provides a consumer for <see cref="MqttVisualization"/>.
/// </summary>
[UsedImplicitly]
public class MqttVisualizationConsumer(ILogger<MqttVisualizationConsumer> logger):
    IConsumer<MqttVisualization>
{
    /// <inheritdoc/> 
    public async Task Consume(ConsumeContext<MqttVisualization> context)
    {
        var mqttPosition = context.Message.AgvPosition;
        string? serialNumber = context.Message.SerialNumber;
        if (string.IsNullOrEmpty(serialNumber))
        {
            logger.LogError("Missing serialNumber in {MessageType} message: {Visualization}", nameof(MqttVisualization), context.Message);
            throw new SerialNumberNotSetException(nameof(MqttVisualization));
        }
        if (mqttPosition == null)
        {
            return;
        }
        logger.LogDebug("New position received for agv {AgvSerialNumber}/{AgvCorrelationId}", serialNumber, serialNumber.ToGuid());
        var agvPosition = new AgvPosition(
            mqttPosition.PositionInitialized,
            (float)mqttPosition.X,
            (float)mqttPosition.Y,
            0f,
            (float)mqttPosition.Theta,
            (float?)mqttPosition.DeviationRange
        );

        await context.Publish(new AgvPositionReceived(serialNumber.ToGuid(), serialNumber, agvPosition));
    }
}