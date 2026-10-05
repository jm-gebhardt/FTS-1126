using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;

/// <summary>
/// Provides a handler for order-related events.
/// </summary>
[UsedImplicitly]
public class OrderEventHandler(IOrderActionStatusCacheProvider actionStatusCache, IAgvRepository agvRepository, ILogger<OrderEventHandler> logger):
    IConsumer<OrderStateReceived>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<OrderStateReceived> context)
    {
        var (correlationId, orderState) = context.Message;
        actionStatusCache.ConcurrentDict.TryGetValue(correlationId, out var previousOrderActionStatus);
        if (orderState.ActionStates.Count == 0)
        {
            return;
        }
        try
        {
            var agv = await agvRepository.GetAgvByCurrentOrderAsync(correlationId);
            ICollection<Task> publishTasks = [];
            foreach (var actionState in orderState.ActionStates)
            {
                var previousState = previousOrderActionStatus?.SingleOrDefault(a => a.ActionId == actionState.ActionId);
                if (previousState == null || previousState.ActionStatus != actionState.ActionStatus)
                {
                    publishTasks.Add(context.Publish(new VehicleActionStatusChanged(correlationId, agv.CorrelationId, actionState.ActionType ?? string.Empty, actionState.ActionStatus)));
                }
            }
            await Task.WhenAll(publishTasks);
            actionStatusCache.ConcurrentDict.AddOrUpdate(correlationId, orderState.ActionStates, (_, _) => orderState.ActionStates);
        }
        catch (AgvNotFoundException)
        {
            logger.LogDebug("Vehicle action status changes not published, order {CorrelationId} not currently run by agv", correlationId);
        }
    }
}