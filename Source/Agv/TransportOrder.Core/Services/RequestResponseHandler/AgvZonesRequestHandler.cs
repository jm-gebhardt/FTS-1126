using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using RemoveAgvFromZone = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RemoveAgvFromZone;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles requests for agvs in zones.
/// </summary>
[UsedImplicitly]
public class AgvZonesRequestHandler(ILogger<AgvZonesRequestHandler> logger, IBus bus, IAgvDb agvDb):
    IConsumer<RemoveAgvFromZones>,
    IConsumer<RemoveAgvFromZone>
{
    /// <summary>
    /// Handles the <see cref="RemoveAgvFromZones"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromZones> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        logger.LogInformation("Removing agv {AgvCorrelationId} from zones", agvCorrelationId);
        var offlineAgv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        var onlineAgv = await agvDb.Agvs.GetAnyOnlineAgv();
        if (onlineAgv == null)
        {
            throw new AgvNotFoundException("No online agv found");
        }

        // trigger removal on device side
        List<Task> publishTasks = offlineAgv.AgvStatus!.ZoneIds!.Select(z => bus.Publish(new Common.Messages.AgvManagement.Commands.RemoveAgvFromZone(onlineAgv.CorrelationId, onlineAgv.SerialNumber, offlineAgv.SerialNumber, z))).ToList();
        // trigger cleanup of agv status and error
        publishTasks.Add(bus.Publish(new RemoveAgvFromZones(agvCorrelationId)));
        // respond
        publishTasks.Add(context.RespondAsync(new Success()));
        await Task.WhenAll(publishTasks);
    }

    /// <summary>
    /// Handles the <see cref="RemoveAgvFromZone"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromZone> context)
    {
        (var agvCorrelationId, string zoneId) = context.Message;
        logger.LogInformation("Removing agv {AgvCorrelationId} from zones", agvCorrelationId);
        var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        bool isInZone = agv.AgvStatus?.ZoneIds?.Contains(zoneId) ?? false;

        if (!isInZone)
        {
            throw new AgvNotInZoneException(zoneId, agvCorrelationId);
        }

        var onlineAgv = await agvDb.Agvs.GetAnyOnlineAgv();

        if (onlineAgv == null)
        {
            throw new AllAgvsOfflineException();
        }

        await bus.Publish(new RemoveAgvFromZone(agvCorrelationId, zoneId));

        await bus.Publish(new Common.Messages.AgvManagement.Commands.RemoveAgvFromZone(onlineAgv.CorrelationId, onlineAgv.SerialNumber, agv.SerialNumber, zoneId));

        await context.RespondAsync(new Success());
    }
}