using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Services;

/// <summary>
/// Consuming a RequestAgvMap message
/// </summary>
public class RequestAgvMapConsumer : IConsumer<RequestAgvMap>
{
    private readonly ILogger<RequestAgvMapConsumer> _logger;

    /// <summary>
    /// Create consumer for agv request map message (<see cref="MqttState"/>)
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    [PublicAPI]
    public RequestAgvMapConsumer(ILogger<RequestAgvMapConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Translate RequestAgvMap content into a MqttRequestMap message
    /// </summary>
    public async Task Consume(ConsumeContext<RequestAgvMap> context)
    {
        await context.PublishMqtt(new MqttRequestMap(context.Message.SerialNumber, context.Message.CorrelationId), context.CancellationToken);
        _logger.LogInformation("Requesting map {MapId}/{MapCorrelationId} for AGV {AgvId}/{CorrelationId}",
            context.Message.MapId, context.Message.MapCorrelationId, context.Message.SerialNumber, context.Message.CorrelationId);
    }
}