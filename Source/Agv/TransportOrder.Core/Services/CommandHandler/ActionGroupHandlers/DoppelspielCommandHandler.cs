using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Domain.Services.Station;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Queue = Gebhardt.Acc.TransportOrder.Domain.Models.Queue.Queue;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;

/// <summary>
/// Handles Doppelspiel-related events and converts them in the corresponding command.
/// </summary>
[UsedImplicitly]
public class DoppelspielCommandHandler(
    IMapper mapper,
    ILogger<DoppelspielCommandHandler> logger,
    IActionGroupRepository actionGroupRepository,
    ISemaphoreProvider<IDoppelspielSemaphore> doppelspielSemaphore,
    IAgvDb agvDb,
    ILoggingService loggingService,
    IStationRepository stationRepository,
    IStationAgvLimitGuard agvLimitGuard,
    IRouteService routeService,
    IQueueManager queueManager) :
    IConsumer<ActionGroupCreated>,
    IConsumer<AgvLeftStation>,
    IConsumer<TriggerDoppelspielPartnerQueueing>,
    IConsumer<PickActionCompleted>,
    IConsumer<AgvPassedDoppelspielTriggerPoint>,
    IConsumer<AgvLeftDoppelspielTrigger>,
    IConsumer<EvacuateOrAssignQueueHead>,
    IConsumer<ActionGroupReassignedOrReset>,
    IConsumer<TryAssignDelayedOrderToAgv>,
    IConsumer<ActionGroupCanceled>,
    IConsumer<ActionAborted>,
    IConsumer<StartActionCommand>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ActionGroupCreated> context)
    {
        var actionGroup = await actionGroupRepository.GetActionGroupAsync(context.Message.ActionGroup.CorrelationId);
        if (actionGroup == null || actionGroup.IsQueued)
        {
            return;
        }
        if (!actionGroup.IsMovingCargo)
        {
            return;
        }

        var nextActionToStart = actionGroup.FirstNotStartedAction;
        if (nextActionToStart == null)
        {
            return;
        }

        if (nextActionToStart.Target.DoppelspielMode == DoppelspielMode.Always)
        {
            logger.LogInformation("Not queueing action group, it will be only picked with Doppelspiel {ActionGroupCorrelationId}", actionGroup.CorrelationId);
            return;
        }

        if (await ReconsiderDoppelspiel(context.Message.ActionGroup))
        {
            actionGroup.ConsiderForDoppelspiel();
        }

        bool delayOrder = false;
        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            ICollection<string> stations = nextActionToStart.Target.Node == null ? nextActionToStart.Target.StationOptions : [nextActionToStart.Target.Node.Name];
            var dropActionGroup = await actionGroupRepository.GetNextDropActionGroupEligibleForDoppelspiel(stations);
            if (dropActionGroup != null)
            {
                delayOrder = true;
                dropActionGroup.RegisterDoppelspiel(ActionType.Drop);
                actionGroup.RegisterDoppelspiel(ActionType.Pick);
                actionGroup.AssignedAgv = dropActionGroup.AssignedAgv;
                var agv = await agvDb.Agvs.GetAgvAsync(dropActionGroup.AssignedAgv.GetValueOrDefault());
                await loggingService.LogOrder(logger, LogLevel.Information, LogCodes.OrderAgvAssigned, dropActionGroup.AssignedAgv.GetValueOrDefault(),
                    actionGroup.CorrelationId, "AGV Assigned",
                    $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
                    new Dictionary<string, object?>
                        { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });
                await actionGroupRepository.UpdateAsync(dropActionGroup);
                await actionGroupRepository.UpdateAsync(actionGroup);
                logger.LogInformation("Registered drop order {DropOrderCorrelationId} to potentially pick order {PickOrderCorrelationId} for Doppelspiel", dropActionGroup.CorrelationId, actionGroup.CorrelationId);
            }
        });

        if (delayOrder)
        {
            return;
        }

        if (!await agvLimitGuard.IsWithinLimitAsync(actionGroup))
        {
            return;
        }

        await context.Publish(new QueueActionGroup(mapper.Map<ActionGroup>(actionGroup), false));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvLeftStation> context)
    {
        // Agv limitation resolved, so try queue next order for this station group (TryQueueDelayedOrder)
        var station = await stationRepository.GetStation(context.Message.CorrelationId);
        if (station.StationGroup.Count == 0)
        {
            // do not queue action groups for stations that are not agv limited
            return;
        }
        List<Guid> stationGroup = [..station.StationGroup, station.Id];
        var actionGroup = await actionGroupRepository.GetNextDelayedDropForStations(stationGroup);
        if (actionGroup == null)
        {
            logger.LogInformation("No delayed order for station group {StationGroup}", string.Join(",", stationGroup));
            return;
        }
        if (!await agvLimitGuard.IsWithinLimitAsync(actionGroup))
        {
            logger.LogInformation("Agv {AgvCorrelationId} left station. Not queuing order {OrderId} / {CorrelationId} that was kept back because the agv limitation is still active", context.Message.AgvCorrelationId, actionGroup.HandlingUnit, actionGroup.CorrelationId);
            return;
        }
        logger.LogInformation("Agv {AgvCorrelationId} left station. Queuing order {OrderId} / {CorrelationId} that was kept back because of agv limitation", context.Message.AgvCorrelationId, actionGroup.HandlingUnit, actionGroup.CorrelationId);
        await context.Publish(new QueueActionGroup(mapper.Map<ActionGroup>(actionGroup), false));
    }

    private async Task<bool> ReconsiderDoppelspiel(ActionGroup actionGroup)
    {
        if (actionGroup.CanRunDoppelspiel)
        {
            return false;
        }
        var dropAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);
        if (dropAction.Target.DoppelspielMode != DoppelspielMode.OrderOrQueueHead)
        {
            return false;
        }
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var agv = await agvDb.Agvs.GetAnyOnlineAgv();
        if (agv == null)
        {
            return false;
        }
        var source = pickAction.Target.Node != null ? pickAction.Target.Node!.Id : actionGroup.Actions.Single(a => a.Sequence == pickAction.Sequence - 1).Target.Node!.Id;
        var target = dropAction.Target.Node != null ? dropAction.Target.Node!.Id : actionGroup.Actions.Single(a => a.Sequence == dropAction.Sequence - 1).Target.Node!.Id;
        var route = await routeService.GetRouteAsync(agv, target, source);
        ICollection<Queue> queues= await queueManager.GetQueuesOnRoute(route.Nodes.Select(n => n.Id).ToList());

        return queues.All(q => q.AgvFillQuantity == 0);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TriggerDoppelspielPartnerQueueing> context)
    {
        // Late Doppelspiel
        var (actionGroupCorrelationId, agvCorrelationId, pickNode) = context.Message;

        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupCorrelationId);

        if (actionGroup.HasQueuedDoppelspielpartner) // has already queued its Doppelspielpartner
        {
            logger.LogInformation("Order {ActionGroupCorrelationId} has already queued DoppelspielPartner", actionGroup.CorrelationId);
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }

        var delayedOrder = await actionGroupRepository.GetNextPickForStation(pickNode);
        if (delayedOrder == null)
        {
            logger.LogInformation("Order {OrderCorrelationId} has no next Doppelspiel pick", actionGroup.CorrelationId);
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }
        if (delayedOrder.Actions.SingleOrDefault(a => a.ActionType == ActionType.Pick)?.Target.DoppelspielMode is (DoppelspielMode.Always or DoppelspielMode.None))
        {
            logger.LogInformation("Order {OrderCorrelationId} target is not eligible for Doppelspiel", actionGroup.CorrelationId);
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }
        if ((await actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agvCorrelationId)).Any(a => a.CorrelationId != actionGroup.CorrelationId))
        {
            logger.LogInformation("Order {OrderCorrelationId} target is not eligible for Doppelspiel, agv {AgvCorrelationId} has already assigned action group", actionGroup.CorrelationId, agvCorrelationId);
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }
        if (!await agvLimitGuard.IsWithinLimitAsync(delayedOrder))
        {
            logger.LogInformation("Order {OrderCorrelationId} Doppelspiel pick {PickOrderCorrelationId} is not started because of station limitation", actionGroup.CorrelationId, delayedOrder.CorrelationId);
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
            return;
        }
        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            delayedOrder.SetDoppelspielpartner(actionGroupCorrelationId, ActionType.Pick, pickNode);
            delayedOrder.Assign(agvCorrelationId);
            actionGroup.SetDoppelspielpartner(delayedOrder.CorrelationId, ActionType.Drop);
            await actionGroupRepository.UpdateAsync(delayedOrder);
            await actionGroupRepository.UpdateAsync(actionGroup);
            await context.Publish(new QueueActionGroup(mapper.Map<ActionGroup>(delayedOrder), false));
            await context.RespondAsync(new DoppelspielPartnerQueued(delayedOrder.CorrelationId));
            logger.LogInformation("Queued order {HostOrder} with pick node {Node} as Doppelspielpartner for Order {DropOrder} during late Doppelspiel", delayedOrder.CorrelationId, pickNode, actionGroup.CorrelationId);
        });
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<PickActionCompleted> context)
    {
        // Queue orders that have pick Doppelspiel mode 'always'
        var (actionGroupCorrelationId, _, _) = context.Message;
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupCorrelationId);
        var dropAction = actionGroup.Actions.First(a => a.ActionType == ActionType.Drop);
        if (dropAction.Target.Node == null)
        {
            return;
        }
        var dropTarget = await stationRepository.GetStation(dropAction.Target.Node!.Id);
        var delayedOrder = await actionGroupRepository.GetNextPickForStation(dropAction.Target.Node!);
        if (delayedOrder == null)
        {
            logger.LogInformation("Order {OrderCorrelationId} has no next Doppelspiel pick at station {StationName}", actionGroup.CorrelationId, dropTarget.Name);
            return;
        }
        if (delayedOrder.Actions.First(a => a.ActionType == ActionType.Pick).Target.DoppelspielMode != DoppelspielMode.Always)
        {
            logger.LogInformation("Delayed order {OrderCorrelationId} for station {StationName} has not DoppelspielMode 'Always'", actionGroup.CorrelationId, dropTarget.Name);
            return;
        }
        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            delayedOrder.SetDoppelspielpartner(actionGroupCorrelationId, ActionType.Pick, dropAction.Target.Node!);
            delayedOrder.Assign(actionGroup.AssignedAgv.GetValueOrDefault());
            actionGroup.SetDoppelspielpartner(delayedOrder.CorrelationId, ActionType.Drop);
            await actionGroupRepository.UpdateAsync(delayedOrder);
            await actionGroupRepository.UpdateAsync(actionGroup);
            await context.Publish(new QueueActionGroup(mapper.Map<ActionGroup>(delayedOrder), false));
            logger.LogInformation("Queued order {HostOrder} with pick node {Node} as Doppelspielpartner for Order {DropOrder}", delayedOrder.CorrelationId, dropTarget.Name, actionGroup.CorrelationId);
        });
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvPassedDoppelspielTriggerPoint> context)
    {
        (_, var agvStatus, var actionGroupInMessage, ICollection<Cargo> _, ICollection<AgvError> _, string targetStationName) = context.Message;
        if (actionGroupInMessage == null)
        {
            logger.LogWarning("Agv without pick drop action group passed Doppelspiel trigger point ({ActionGroup})", actionGroupInMessage?.CorrelationId);
            return;
        }
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupInMessage.CorrelationId);
        if (!actionGroup.IsMovingCargo)
        {
            logger.LogWarning("Agv without pick drop action group passed Doppelspiel trigger point ({ActionGroup})", actionGroupInMessage.CorrelationId);
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
            string? sap = action.GetSap();
            if (sap == null)
            {
                logger.LogError("SAP not given for manual pick drop action group");
                return;
            }
            await context.Publish(new ManualActionPassedDoppelspielTrigger(sap, action.Target.Node!.Name, !isDropping, actionGroup.HandlingUnit!, action.CorrelationId, actionGroup.AssignedAgv!.Value));
        }
        if (!isDropping)
        {
            await context.Publish(new PickingAgvPassedDoppelspielTriggerPoint(action.Target.Node!, actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId));
            return;
        }
        var dropTarget = await stationRepository.GetStation(action.Target.Node!.Id);
        if (!actionGroup.CanRunDoppelspiel || action.Target.DoppelspielMode == DoppelspielMode.None)
        {
            logger.LogInformation("Order {OrderCorrelationId} is not considered for Doppelspiel at station {StationName}", actionGroup.CorrelationId, dropTarget.Name);
            await context.Publish(new DroppingAgvPassedDoppelspielTriggerPoint(action.Target.Node!, actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId, null));
            return;
        }
        var delayedOrder = await actionGroupRepository.GetNextPickForStation(action.Target.Node!);
        if (delayedOrder == null)
        {
            logger.LogInformation("Order {OrderCorrelationId} has no next Doppelspiel pick at station {StationName}", actionGroup.CorrelationId, dropTarget.Name);
            await context.Publish(new DroppingAgvPassedDoppelspielTriggerPoint(action.Target.Node!, actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId, null));
            return;
        }
        var agv = await agvDb.Agvs.GetAgvAsync(agvStatus.CorrelationId);
        var agvSettings = agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        if (agv.CheckMustCharge(agvSettings))
        {
            actionGroup.UnregisterFromDoppelspiel(ActionType.Drop);
            await actionGroupRepository.UpdateAsync(actionGroup);
            logger.LogInformation("Order {OrderCorrelationId} has next Doppelspiel pick at station {StationName}, but agv {AgvCorrelationId} must charge", actionGroup.CorrelationId, dropTarget.Name, agv.CorrelationId);
            await context.Publish(new DroppingAgvPassedDoppelspielTriggerPoint(action.Target.Node!, actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId, null));
            await QueueUnmatchedDoppelspielPicks(context, action.Target.Node, actionGroup.CorrelationId);
            return;
        }
        if (!await agvLimitGuard.IsWithinLimitAsync(delayedOrder))
        {
            logger.LogInformation("Order {OrderCorrelationId} Doppelspiel pick {PickOrderCorrelationId} is not started because of station limitation", actionGroup.CorrelationId, delayedOrder.CorrelationId);
            await context.Publish(new DroppingAgvPassedDoppelspielTriggerPoint(action.Target.Node!, actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId, null));
            return;
        }

        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            delayedOrder.SetDoppelspielpartner(actionGroup.CorrelationId, ActionType.Pick, action.Target.Node!);
            delayedOrder.Assign(actionGroup.AssignedAgv.GetValueOrDefault());
            actionGroup.SetDoppelspielpartner(delayedOrder.CorrelationId, ActionType.Drop);
            await actionGroupRepository.UpdateAsync(delayedOrder);
            await actionGroupRepository.UpdateAsync(actionGroup);
            await context.Publish(new QueueActionGroup(mapper.Map<ActionGroup>(delayedOrder), false));
            logger.LogInformation("Queued order {HostOrder} with pick node {Node} as Doppelspielpartner for Order {DropOrder} during passing of Doppelspiel trigger", delayedOrder.CorrelationId, dropTarget.Name, actionGroup.CorrelationId);
            await context.Publish(new DroppingAgvPassedDoppelspielTriggerPoint(action.Target.Node!, actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId, delayedOrder.CorrelationId));
        });
    }

    private async Task QueueUnmatchedDoppelspielPicks(IPublishEndpoint context, GraphNode node, Guid? dropOrder = null)
    {
        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            var unmatchedPick = await actionGroupRepository.GetNextUnmatchedDoppelspielPick(node, dropOrder);
            if (unmatchedPick is { IsQueued: false })
            {
                unmatchedPick.UnregisterFromDoppelspiel(ActionType.Pick);
                await actionGroupRepository.UpdateAsync(unmatchedPick);
                logger.LogInformation("Trigger queueing of unmatch pick order {ActionGroupCorrelationId}", unmatchedPick.CorrelationId);
                await context.Publish(new QueueActionGroup(mapper.Map<ActionGroup>(unmatchedPick), false));
            }
        });
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvLeftDoppelspielTrigger> context)
    {
        string targetStationName = context.Message.TargetStationName;
        await QueueUnmatchedDoppelspielPicks(context, targetStationName.ToGraphNode());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<EvacuateOrAssignQueueHead> context)
    {
        // Consider drop order for Doppelspiel
        var currentAction = await actionGroupRepository.GetCurrentAgvActionAsync(context.Message.TriggeringAgv);
        if (currentAction == null || currentAction.ActionType != ActionType.Drop)
        {
            return;
        }
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(currentAction.ParentId);
        if (actionGroup.CanRunDoppelspiel)
        {
            return;
        }
        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            actionGroup.ConsiderForDoppelspiel();
            await actionGroupRepository.UpdateAsync(actionGroup);
            logger.LogInformation("Considered order {ActionGroupCorrelationId} for Doppelspiel", actionGroup.CorrelationId);
        });
    }

    /// <inheritdoc/>
    public async  Task Consume(ConsumeContext<ActionGroupReassignedOrReset> context)
    {
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);
        if (!actionGroup.IsMovingCargo)
        {
            return;
        }
        if (actionGroup.Actions.Where(a => a.ActionType == ActionType.Pick).All(a => a.CompletedDate == null))
        {
            return;
        }
        if (actionGroup is { IsRegisteredForDoppelspielDrop: true, HasQueuedDoppelspielpartner: false })
        {
            logger.LogInformation("Unregistering drop of reset or reassigned action group {ActionGroupCorrelationId} from Doppelspiel", actionGroup.CorrelationId);
            actionGroup.UnregisterFromDoppelspiel(ActionType.Drop);
            await actionGroupRepository.UpdateAsync(actionGroup);
        }
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TryAssignDelayedOrderToAgv> context)
    {
        var currentAction = await actionGroupRepository.GetCurrentAgvActionAsync(context.Message.CorrelationId);
        if (currentAction == null || currentAction.ActionType != ActionType.Drop)
        {
            logger.LogInformation("{Message}: No action group found for agv {AgvCorrelationId}", nameof(TryAssignDelayedOrderToAgv), context.Message.CorrelationId);
            await context.RespondAsync(new AssignedDelayedOrder(null));
            return;
        }
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(currentAction.ParentId);
        if (currentAction.Target.DoppelspielMode != DoppelspielMode.OrderOrQueueHead)
        {
            logger.LogInformation("{Message}: Action group {ActionGroupCorrelationId} for agv {AgvCorrelationId} drop target is not in mode OrderOrQueueHead", nameof(TryAssignDelayedOrderToAgv), actionGroup.CorrelationId, context.Message.CorrelationId);
            await context.RespondAsync(new AssignedDelayedOrder(null));
            return;
        }
        var pickActionGroup = await actionGroupRepository.GetNextPickForStation(currentAction.Target.Node!, context.Message.OrdersToIgnore);
        if (pickActionGroup == null)
        {
            logger.LogInformation("{Message}: No delayed pick for agv {AgvCorrelationId} running actionGroup {ActionGroupCorrelationId} ", nameof(TryAssignDelayedOrderToAgv), context.Message.CorrelationId, actionGroup.CorrelationId);
            await context.RespondAsync(new AssignedDelayedOrder(null));
            return;
        }
        if (!await agvLimitGuard.IsWithinLimitAsync(pickActionGroup))
        {
            logger.LogInformation("Order {OrderCorrelationId} Doppelspiel pick {PickOrderCorrelationId} is not started because of station limitation", actionGroup.CorrelationId, pickActionGroup.CorrelationId);
            await context.RespondAsync(new AssignedDelayedOrder(null));
            return;
        }
        await doppelspielSemaphore.ExecuteLockedAsync(async () =>
        {
            pickActionGroup.SetTargetAndAssign(context.Message.AssignableAgv, ActionType.Pick,  currentAction.Target.Node!);
            await actionGroupRepository.UpdateAsync(pickActionGroup);
            await actionGroupRepository.UpdateAsync(actionGroup);
            await context.Publish(new QueueActionGroup(mapper.Map<ActionGroup>(pickActionGroup), false));
            logger.LogInformation("Queued order {HostOrder} with pick node {Node} during assignment of delayed order to head of queue", pickActionGroup.CorrelationId, currentAction.Target.Node!.Name);
            await context.RespondAsync(new AssignedDelayedOrder(pickActionGroup.CorrelationId));
        });
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ActionGroupCanceled> context)
    {
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.ActionGroup.CorrelationId);
        if (!actionGroup.IsMovingCargo)
        {
            return;
        }
        await CancelDoppelspiel(context, actionGroup);
    }

    private async Task CancelDoppelspiel(IPublishEndpoint context, Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        if (!actionGroup.IsMovingCargo)
        {
            return;
        }
        var dropAction = actionGroup.Actions.First(a => a is { ActionType: ActionType.Drop, ShouldSkip: false });
        if (dropAction is { CompletedDate: null, IsRegisteredForDoppelspiel: true })
        {
            await QueueUnmatchedDoppelspielPicks(context, dropAction.Target.Node!, actionGroup.CorrelationId);
        }
        var pickAction = actionGroup.Actions.First(a => a is { ActionType: ActionType.Pick, ShouldSkip: false, DoppelspielPartner: null });
        {
            await doppelspielSemaphore.ExecuteLockedAsync(async () =>
            {
                var unmatchedDrop = await actionGroupRepository.GetNextUnmatchedDoppelspielDrop(pickAction.Target.Node!, actionGroup.CorrelationId);
                if (unmatchedDrop != null)
                {
                    logger.LogInformation("Unregistering drop of order {OrderId} from Doppelspiel", unmatchedDrop.CorrelationId);
                    unmatchedDrop.UnregisterFromDoppelspiel(ActionType.Drop);
                    await actionGroupRepository.UpdateAsync(unmatchedDrop);
                }
            });
        }
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        (_, var action, bool isTemporary) = context.Message;

        if (isTemporary)
        {
            return;
        }
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByIdAsync(action.ParentId);
        await CancelDoppelspiel(context, actionGroup);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<StartActionCommand> context)
    {
        // Station Limitation: Reserve station with station group on start of drop, if drop is at station group
        var action = context.Message.Action;
        var agvCorrelationId = context.Message.CorrelationId;
        if (action.ActionType != ActionType.Drop || action.Target.Node == null)
        {
            return;
        }
        var dropStation = await stationRepository.GetStation(action.Target.Node!.Id);
        if (dropStation.StationGroup.Count > 0)
        {
            logger.LogInformation("{Message}: Agv {AgvCorrelationId} started {ActionType} action {ActionCorrelationId} at station {Station}, reserve target station {TargetStation}",
                nameof(StartActionCommand), agvCorrelationId, action.ActionType.ToString(), action.CorrelationId, action.Nodes.FirstOrDefault()?.Name, action.Target.Node.Name);
            await context.Publish(new ReserveStation(action.Target.Node, agvCorrelationId));
        }
    }
}