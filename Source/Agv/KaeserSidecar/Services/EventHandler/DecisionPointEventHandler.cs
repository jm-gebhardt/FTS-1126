using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
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
    ITargetSelector selector) : 
    IConsumer<AgvPassedDecisionPoint1>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedDecisionPoint1"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedDecisionPoint1> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, int consideredStations) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.NodeId != string.Empty))
        {
            logger.LogWarning("Skip target selection: Agv {AgvCorrelationId} passed decision point without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        string? targetNode = await selector.Select(consideredStations);
        if (targetNode == null)
        {
            logger.LogError("Could not select target for action group {ActionGroupId}", actionGroup.CorrelationId);
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.Target.NodeId == string.Empty);
        await SetTarget(context, targetNode, agvStatus, actionGroup, action!.CorrelationId);
        await context.Publish(new OccupyStation(targetNode, agvStatus.CorrelationId));
        logger.LogInformation("OccupyStation published for agv {agv}, station {station}", agvStatus.CorrelationId, targetNode);
    }

    private async Task SetTarget(IPublishEndpoint context, string target, AgvStatus agvStatus, ActionGroup actionGroup, Guid action)
    {
        logger.LogInformation("Selecting target {Target} for agv {Agv} running action group {ActionGroup}", target, agvStatus.CorrelationId, actionGroup.CorrelationId);
        await context.Publish(new SetTargetForAction(action, actionGroup.CorrelationId, agvStatus.CorrelationId, target));
    }
}