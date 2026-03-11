using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;

/// <summary>
/// Handles action-related events.
/// </summary>
[UsedImplicitly]
public class ActionEventHandler(ILogger<ActionEventHandler> logger, IAgvRepository agvRepository) :
    IConsumer<ActionAborted>,
    IConsumer<ActionCompletedByAgv>,
    IConsumer<ActionAbortedByAgv>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        var action = context.Message.Action;
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);
        logger.LogInformation("{Message} received for {ActionType} action of agv {AgvCorrelationId}", nameof(ActionAborted), action.ActionType, agvCorrelationId);

        if (action.ActionType == ActionType.Charge)
        {
            await context.Publish(new StopChargingRequested(agv.CurrentOrder ?? action.CorrelationId));
            return;
        }
        await context.Publish(new OrderCancellationRequested(agv.CurrentOrder ?? action.CorrelationId));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ActionCompletedByAgv> context)
    {
        var (currentOrder, agvCorrelationId) = context.Message;
        logger.LogInformation("Order {ActionCorrelationId} completed by agv {AgvCorrelationId}, remove current order reference", currentOrder, agvCorrelationId);
        await RemoveOrderFromAgv(agvCorrelationId);
    }

    private async Task RemoveOrderFromAgv(Guid agvCorrelationId)
    {
        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);
        agv.CurrentOrder = null;
        agv.CurrentFleetManagerActionId = null;
        await agvRepository.AddOrUpdateAsync(agv);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ActionAbortedByAgv> context)
    {
        var (currentOrder, agvCorrelationId) = context.Message;
        logger.LogInformation("Order {ActionCorrelationId} aborted by agv {AgvCorrelationId}, remove current order reference", currentOrder, agvCorrelationId);
        await RemoveOrderFromAgv(agvCorrelationId);
    }
}