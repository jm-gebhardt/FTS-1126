using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides a handler for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionEventHandler :
    IConsumer<ActionGroupCreatedForAgv>,
    IConsumer<ActionCompleted>,
    IConsumer<StartingActionForAgvFailed>,
    IConsumer<NonChargingActionCompleted>,
    IConsumer<NodesSelectedForAction>,
    IConsumer<ActionAbortionCompleted>,
    IConsumer<ActionCanceled>,
    IConsumer<ActionDurationEstimated>,
    IConsumer<AutomaticActionWithdrawn>
{
    private readonly ILogger<ActionEventHandler> _logger;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;
    private readonly IAgvDb _agvDb;

    /// <summary>
    /// Initializes a new <see cref="ActionEventHandler"/> instance.
    /// </summary>
    public ActionEventHandler(
        IActionGroupRepository actionGroupRepository,
        IAgvDb agvDb,
        ISettingsProvider<AgvSettings> settingsProvider,
        ILogger<ActionEventHandler> logger
    )
    {
        _actionGroupRepository = actionGroupRepository;
        _agvDb = agvDb;
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCreatedForAgv"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCreatedForAgv> context)
    {
        var (actionGroupInMessage, assignedAgv) = context.Message;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupInMessage.CorrelationId);

        var chargeAction = await _actionGroupRepository.GetCurrentAgvActionAsync(assignedAgv);
        if (chargeAction?.ActionType == ActionType.Charge)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(assignedAgv);
            var agvSettings = _settingsProvider.GetSettings();
            if (agv.AgvStatus?.BatteryCharge >= agvSettings.MinChargeUp)
            {
                _logger.LogInformation("Aborting charging for action group: {ActionGroupCorrelationId}", chargeAction.ParentId);
                await context.Publish(new AbortChargeAction(assignedAgv));
                return;
            }
        }

        var firstNotStartedAction = actionGroup.FirstNotStartedAction;
        var action = actionGroupInMessage.Actions.First(a => a.CorrelationId == firstNotStartedAction!.CorrelationId);
        _logger.LogInformation("Received action group created for agv {AgvCorrelationId}, action group {ActionGroupCorrelationId}, starting action {Action} next.", assignedAgv, action.ParentId, action.CorrelationId);
        await context.Publish(new SelectNodesForAction(action, assignedAgv));
    }

    /// <summary>
    /// Handle <see cref="ActionCompleted"/> from DeviceCommunication
    /// </summary>
    public async Task Consume(ConsumeContext<ActionCompleted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);

        await context.Publish(new CompleteActionOfActionGroup(actionCorrelationId, actionGroup.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="StartingActionForAgvFailed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<StartingActionForAgvFailed> context)
    {
        await context.Publish(new HandleAgvActionStartFailure(context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="NonChargingActionCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<NonChargingActionCompleted> context)
    {
        await context.Publish(new HandleActionCompletion(context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="NodesSelectedForAction"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<NodesSelectedForAction> context)
    {
        await context.Publish(new EstimateActionGroupDuration(context.Message.Action, context.Message.AssignedAgv, context.Message.Action.ParentId));
    }

    /// <inheritdoc cref="ActionDurationEstimated" />
    public async Task Consume(ConsumeContext<ActionDurationEstimated> context)
    {
        await context.Publish(new PrepareAgvForStartingAction(context.Message.Action, context.Message.AgvId));
    }

    /// <summary>
    /// Handles the <see cref="ActionAbortionCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAbortionCompleted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        Guid? assignedAgv = await _actionGroupRepository.GetAgvAssignedToAction(actionCorrelationId);
        if (assignedAgv == null)
        {
            _logger.LogError("Could not complete action abortion for action {Action}, no assigned agv", actionCorrelationId);
            return;
        }
        await context.Publish(new HandleActionCompletion(assignedAgv.Value));
    }

    /// <summary>
    /// Handles the <see cref="ActionCanceled"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionCanceled> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        Guid? assignedAgv = await _actionGroupRepository.GetAgvAssignedToAction(actionCorrelationId);
        if (assignedAgv == null)
        {
            _logger.LogError("Could not complete action cancellation for action {Action}, no assigned agv", actionCorrelationId);
            return;
        }
        await context.Publish(new HandleActionCompletion(assignedAgv.Value));
    }

    /// <summary>
    /// Handles the <see cref="AutomaticActionWithdrawn"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AutomaticActionWithdrawn> context)
    {
        await context.Publish(new FreeNode(context.Message.TargetNodeId));
    }
}