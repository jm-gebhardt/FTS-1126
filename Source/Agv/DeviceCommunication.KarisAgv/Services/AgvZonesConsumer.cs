using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Services;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Handler for agv zones related commands.
/// </summary>
[UsedImplicitly]
public class AgvZonesConsumer(ILogger<AgvZonesConsumer> logger, IZoneRefreshStateService zonesRefreshStateService) :
    IConsumer<RequestAgvZones>,
    IConsumer<RemoveAgvFromZone>,
    IConsumer<AgvWentOfflineInZones>,
    IConsumer<AgvWentOnline>

{
    /// <summary>
    /// Handles the <see cref="RequestAgvZones"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RequestAgvZones> context)
    {
        await context.PublishMqtt(new MqttRequestZones(context.Message.SerialNumber, Guid.NewGuid()), context.CancellationToken);
        logger.LogInformation("Request AgvZones from AGV {AgvId} / {AgvCorrelationId} received", context.Message.SerialNumber, context.CorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="RemoveAgvFromZone"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromZone> context)
    {
        await context.PublishMqtt(new MqttRequestZoneRemoveAgv(context.Message.SerialNumber, Guid.NewGuid(), context.Message.AgvToRemove, context.Message.ZoneId), context.CancellationToken);
        logger.LogInformation("{MessageType} for AGV {AgvId} / {AgvCorrelationId} received: zone id {ZoneId} agv to remove {AgvToRemove}", nameof(RemoveAgvFromZone), context.Message.SerialNumber, context.CorrelationId, context.Message.ZoneId, context.Message.AgvToRemove);
    }

    /// <summary>
    /// Handles the <see cref="AgvWentOfflineInZones"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvWentOfflineInZones> context)
    {
        logger.LogInformation("{MessageType} for AGV {AgvId} / {AgvCorrelationId} received", nameof(AgvWentOfflineInZones), context.Message.SerialNumber, context.Message.CorrelationId);
        await zonesRefreshStateService.AddOfflineAgvInZones(context.Message.CorrelationId, context.Message.SerialNumber, context.Message.Zones, context.Message.CurrentOnlineAgvs);
    }

    /// <summary>
    /// Handles the <see cref="AgvWentOnline"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvWentOnline> context)
    {
        logger.LogInformation("{MessageType} for AGV {AgvCorrelationId} received", nameof(AgvWentOnline), context.Message.CorrelationId);
        await zonesRefreshStateService.SetAgvBackOnline(context.Message.CorrelationId);
    }
}