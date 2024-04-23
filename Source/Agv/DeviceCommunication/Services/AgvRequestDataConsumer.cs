using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Services;

/// <summary>
/// Execute processing of a agv data request
/// </summary>
[UsedImplicitly]
public class AgvRequestDataConsumer : IConsumer<RequestAgvData>
{
    private readonly ILogger<AgvRequestDataConsumer> _logger;

    /// <summary>
    /// Handle requesting AGV data by master control.
    /// </summary>
    public AgvRequestDataConsumer(ILogger<AgvRequestDataConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle requesting AGV data on receiving message <see cref="RequestAgvData"/> from master control.
    /// </summary>
    /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
    public async Task Consume(ConsumeContext<RequestAgvData> context)
    {
        await context.PublishMqtt(new MqttRequestData(context.Message.SerialNumber, Guid.NewGuid()), context.CancellationToken);

        _logger.LogInformation("{MessageType} from AGV {AgvId} / {CorrelationId} received", nameof(RequestAgvData), context.Message.SerialNumber, context.CorrelationId);
    }
}