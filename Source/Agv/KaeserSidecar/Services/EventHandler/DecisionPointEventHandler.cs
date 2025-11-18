using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles messages related to the decision trigger point
/// </summary>
[UsedImplicitly]
public class DecisionPointEventHandler(
    ILogger<DecisionPointEventHandler> logger,
    ITargetSelector selector,
    KaeserGraphConfiguration configuration) :
    IConsumer<AgvPassedDecisionPoint1>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedDecisionPoint1"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedDecisionPoint1> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, ICollection<Guid> consideredStations, bool useJamState) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.Node != null))
        {
            logger.LogWarning("Skip target selection: Agv {AgvCorrelationId} passed decision point without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        var moveAction = actionGroup.Actions.FirstOrDefault(a => a.ActionType == ActionType.Move);
        if (moveAction == null || moveAction.Target.Node?.Name != configuration.LongLoopMoveTargetNode)
        {
            logger.LogWarning("Skip target selection: Agv {AgvCorrelationId} passed decision point without without pick drop action group for long loop ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.Target.Node == null);
        if (action == null)
        {
            logger.LogWarning("Skip target selection: Agv {AgvCorrelationId} passed decision point without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }
        selector.SetUseJamState(useJamState);
        selector.SetStationCount(consideredStations);
        await selector.Enqueue(action.CorrelationId, agvStatus.CorrelationId);
    }
}