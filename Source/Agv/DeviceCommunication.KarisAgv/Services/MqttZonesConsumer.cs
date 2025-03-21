using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides handlers for zones sent by the agv.
/// </summary>
[UsedImplicitly]
public class MqttZonesConsumer(ILogger<MqttZonesConsumer> logger):
    IConsumer<MqttZones>
{
    /// <summary>
    /// Handles the <see cref="MqttZones"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<MqttZones> context)
    {
        var parser = new ZonesParser(context.Message);
        logger.LogInformation("Incoming MqttZones: {AgvSerialNumber} / {CorrelationId}", context.Message.SerialNumber, context.Message.RequestId);
        try
        {
            await context.Publish(new AgvZonesReceived(parser.SerialNumber.ToGuid(),
                parser.SerialNumber,
                parser.AgvZones), context.CancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Message {MessageType} has invalid content: {MqttZones} / {CorrelationId}", nameof(MqttZones), context.Message, context.CorrelationId);
            throw;
        }
    }
}