using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Handler for agv zones related commands.
/// </summary>
[UsedImplicitly]
public class AgvZonesConsumer(ILogger<AgvZonesConsumer> logger) :
    IConsumer<RequestAgvZones>,
    IConsumer<RemoveAgvFromZone>

{
    /// <summary>
    /// Handles the <see cref="RequestAgvZones"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RequestAgvZones> context)
    {
        await context.PublishMqtt(new MqttRequestZones(context.Message.SerialNumber, Guid.NewGuid()), context.CancellationToken);
        logger.LogInformation("Request AgvZones from AGV {AgvId} / {CorrelationId} received", context.Message.SerialNumber, context.CorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="RemoveAgvFromZone"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromZone> context)
    {
        await context.PublishMqtt(new MqttRequestZoneRemoveAgv(context.Message.SerialNumber, Guid.NewGuid(), context.Message.AgvToRemove, context.Message.ZoneId), context.CancellationToken);
        logger.LogInformation("{MessageType} for AGV {AgvId} / {CorrelationId} received: zone id {ZoneId} agv to remove {AgvToRemove}", nameof(RemoveAgvFromZone), context.Message.SerialNumber, context.CorrelationId, context.Message.ZoneId, context.Message.AgvToRemove);
    }
}