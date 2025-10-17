using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles messages related to the short loop waiting trigger point.
/// </summary>
[UsedImplicitly]
public class ShortLoopWaitingTriggerPointEventHandler(
    ILogger<ShortLoopWaitingTriggerPointEventHandler> logger,
    KaeserGraphConfiguration configuration,
    IWaitingHandler waitingHandler) :
    IConsumer<AgvPassedShortLoopWaitingHandler>,
    IConsumer<AgvLeftShortLoopWaitingHandler>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedShortLoopWaitingHandler"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedShortLoopWaitingHandler> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.Node != null))
        {
            logger.LogWarning("Skip waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        var moveAction = actionGroup.Actions.FirstOrDefault(a => a.ActionType == ActionType.Move);
        if (moveAction == null || (moveAction.Target.Node?.Name != configuration.LeftShortLoopWaitingNode && moveAction.Target.Node?.Name != configuration.RightShortLoopWaitingNode))
        {
            logger.LogWarning("Skip waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler without without pick drop action group for short loop ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }

        if (moveAction.StartedDate == null && moveAction.Sequence == 1)
        {
            // Prevent order selection before even reaching the waiting point if agv is going to drop at PSX
            logger.LogWarning("Skip waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler before picking at AKL station ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }

        var incompleteAction = actionGroup.Actions.FirstOrDefault(a => a.Target.Node == null);
        if (incompleteAction == null)
        {
            logger.LogWarning("Skip waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler without without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }

        // Use current time for order of action waiting handling to ensure competition with long loop
        // is according to trigger entry time, i.e. order of arrival
        await waitingHandler.StartHandling(incompleteAction.CorrelationId, agvStatus.CorrelationId, DateTime.UtcNow);
    }

    /// <summary>
    /// Handles the <see cref="AgvLeftShortLoopWaitingHandler"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvLeftShortLoopWaitingHandler> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _) = context.Message;

        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick))
        {
            logger.LogWarning("Skip removal of waiting order : Agv {AgvCorrelationId} passed short loop waiting handler without  pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }

        string aplSourceNode = configuration.StationMapping[configuration.PrismatAplSource];
        string aplTargetNode = configuration.StationMapping[configuration.PrismatAplTarget];

        if (actionGroup.Actions.Any(a => a.Target.Node?.Name == aplSourceNode || a.Target.Node?.Name == aplTargetNode))
        {
            logger.LogWarning("Skip removal of waiting order : Agv {AgvCorrelationId} passed short loop waiting handler with long loop action group ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);
            return;
        }

        var action = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Started || configuration.ConveyorStationMapping.ContainsKey(a.Target.Node!.Name));
        if (action == null)
        {
            logger.LogWarning("Skip removal of waiting order : Agv {AgvCorrelationId} passed short loop waiting handler w/o action heading towards short loop we target station or started action ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }

        await waitingHandler.StopHandling(action.CorrelationId);
    }
}