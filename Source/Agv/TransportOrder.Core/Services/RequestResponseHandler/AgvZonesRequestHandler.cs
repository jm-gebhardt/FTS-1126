using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles requests for agvs in zones.
/// </summary>
[UsedImplicitly]
public class AgvZonesRequestHandler(ILogger<AgvZonesRequestHandler> logger, IBus bus, IAgvDb agvDb):
    IConsumer<RemoveAgvFromZones>
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
        ICollection<Task> publishTasks = offlineAgv.AgvStatus!.ZoneIds!.Select(z => bus.Publish(new RemoveAgvFromZone(onlineAgv.CorrelationId, onlineAgv.SerialNumber, offlineAgv.SerialNumber, z))).ToList();
        // trigger cleanup of agv status and error
        publishTasks.Add(bus.Publish(new RemoveAgvFromZones(agvCorrelationId)));
        // respond
        publishTasks.Add(context.RespondAsync(new Success()));
        await Task.WhenAll(publishTasks);
    }
}