using Gebhardt.Acc.TransportOrder.Domain;
using JetBrains.Annotations;
using MassTransit;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using RemoveAgvFromStation = Gebhardt.Acc.Common.Messages.Stations.Commands.RemoveAgvFromStation;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for agv station related requests.
/// </summary>
[UsedImplicitly]
public class AgvStationRequestHandler(
    IAgvDb agvDb,
    IBus bus) :
    IConsumer<Messages.Requests.Stations.RemoveAgvFromStation>
{
    /// <summary>
    /// Handles the <see cref="Messages.Requests.Stations.RemoveAgvFromStation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<Messages.Requests.Stations.RemoveAgvFromStation> context)
    {
        (string stationName, var agvId) = context.Message;

        var agvToRemove = await agvDb.Agvs.TryGetAgvAsync(agvId);

        if (agvToRemove == null)
        {
            throw new AgvNotFoundException(agvId);
        }

        var onlineAgv = await agvDb.Agvs.GetAnyOnlineAgv();

        if (onlineAgv == null)
        {
            throw new AllAgvsOfflineException();
        }

        await bus.Publish(new RemoveAgvFromStation(stationName, agvToRemove.SerialNumber, onlineAgv.SerialNumber));

        await context.RespondAsync(new Success());
    }
}