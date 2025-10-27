using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
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
public class AgvZonesRequestHandler(ILogger<AgvZonesRequestHandler> logger, IBus bus, IAgvDb agvDb, ILoggingService loggingService) :
    IConsumer<RemoveAgvFromAllZones>,
    IConsumer<RemoveAgvFromZone>
{
    /// <summary>
    /// Handles the <see cref="RemoveAgvFromAllZones"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromAllZones> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        string user = context.Message.User;
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
        publishTasks.Add(bus.Publish(new RemoveAgvFromAllZones(agvCorrelationId, user)));
        // respond
        publishTasks.Add(context.RespondAsync(new Success()));
        await loggingService.LogAgv(logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvCorrelationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvRemoveFromZone } });
        await Task.WhenAll(publishTasks);
    }

    /// <summary>
    /// Handles the <see cref="RemoveAgvFromZone"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromZone> context)
    {
        (var agvCorrelationId, string zoneId, string user) = context.Message;
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

        if (agv.IsOnline)
        {
            await bus.Publish(new RemoveAgvFromZone(agvCorrelationId, zoneId, user));
        }

        await bus.Publish(new Common.Messages.AgvManagement.Commands.RemoveAgvFromZone(onlineAgv.CorrelationId, onlineAgv.SerialNumber, agv.SerialNumber, zoneId));
        await loggingService.LogAgv(logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvCorrelationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvRemoveFromZone } });

        await context.RespondAsync(new Success());
    }
}