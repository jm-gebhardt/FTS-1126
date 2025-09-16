using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Extensions;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles Doppelspiel trigger related events.
/// </summary>
[UsedImplicitly]
public class DoppelspielTriggerPointEventHandler(
    ILogger<DoppelspielTriggerPointEventHandler> logger,
    IPrismatCommunicationHostOrderRepository hostOrderRepository,
    IAklAgvLimitGuard aklAgvLimitGuard,
    KaeserGraphConfiguration configuration,
    IAgvSettingsService agvSettingsService
) :
    IConsumer<AgvPassedDoppelspielTriggerPoint>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedDoppelspielTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedDoppelspielTriggerPoint> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, string targetStationName) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick))
        {
            logger.LogWarning("Agv without pick drop action group passed Doppelspiel trigger point ({ActionGroup})", actionGroup?.CorrelationId);
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.State is ActionState.Started or ActionState.Completed && a.Target.Node?.Name == targetStationName);
        if (action == null)
        {
            logger.LogWarning("Agv without started action to station {TargetStation} with Doppelspiel passed Doppelspiel trigger point. Current action group is: ({ActionGroup})", targetStationName, actionGroup.CorrelationId);
            foreach (var a in actionGroup.Actions)
            {
                logger.LogWarning("Action {ActionType} is in State {State} and heads to {Target}", a.ActionType, a.State, a.Target.Node?.Name);
            }
            return;
        }

        bool isDropping = action is { ActionType: ActionType.Drop };

        if (actionGroup.IsManual)
        {
            await HandleManualPickDropActionGroup(context, actionGroup, action, !isDropping);
            return;
        }
        var hostOrder = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
        if (isDropping)
        {
            List<string>? sources = configuration.DoppelspielPickTargets.GetValueOrDefault(hostOrder.Target);
            var nextPickOrder = sources != null ? await hostOrderRepository.GetNextPickForSources(sources) : null;
            bool agvHasCriticalBatteryLevel = await agvSettingsService.CheckAgvHasCriticalBatteryLevel(agvStatus.CorrelationId);
            Guid? nextPickOrderId = nextPickOrder == null || nextPickOrder.IsQueued ? null : nextPickOrder.CorrelationId;
            bool shouldPickNext = !agvHasCriticalBatteryLevel && nextPickOrderId != null && await CanBeUsedForDoppelspiel(nextPickOrder);
            var pickNode = shouldPickNext ? action.Target.Node : null;
            logger.LogInformation("Dropping agv {AgvCorrelationId} passed Doppelspiel trigger point and will drop at {Target} and pick next {ShouldPick} order {PickOrderCorrelationId} and has critical battery level {HasLowBatteryLevel}", actionGroup.AssignedAgv!.Value, action.Target.Node?.Name, shouldPickNext, nextPickOrderId, agvHasCriticalBatteryLevel);
            await context.Publish(new DroppingAgvPassedDoppelspielTriggerPoint(action.Target.Node!, actionGroup.AssignedAgv!.Value, hostOrder.CorrelationId, nextPickOrderId, shouldPickNext, pickNode));
            return;
        }
        logger.LogInformation("Picking agv {AgvCorrelationId} passed Doppelspiel trigger point and will pick at {Target} ", actionGroup.AssignedAgv!.Value, action.Target.Node!.Name);
        await context.Publish(new PickingAgvPassedDoppelspielTriggerPoint(action.Target.Node!, actionGroup.AssignedAgv!.Value, hostOrder.CorrelationId));
    }

    private async Task HandleManualPickDropActionGroup(IPublishEndpoint context, ActionGroup actionGroup, AgvAction action, bool isPicking)
    {
        string? sap = action.GetSap();
        if (sap == null)
        {
            logger.LogError("SAP not given for manual pick drop action group");
            return;
        }
        await context.Publish(new ManualActionPassedDoppelspielTrigger(sap, action.Target.Node!.Name, isPicking, actionGroup.HandlingUnit!, action.CorrelationId, actionGroup.AssignedAgv!.Value));
    }

    private Task<bool> CanBeUsedForDoppelspiel(HostOrder? hostOrder)
    {
        if (hostOrder?.IsQueued == true)
        {
            return Task.FromResult(false);
        }
        if (configuration.UseAklAgvLimit.HasValue && !configuration.UseAklAgvLimit.Value)
        {
            return Task.FromResult(true);
        }

        return aklAgvLimitGuard.IsWithinAklAgvLimitAsync(hostOrder);
    }
}