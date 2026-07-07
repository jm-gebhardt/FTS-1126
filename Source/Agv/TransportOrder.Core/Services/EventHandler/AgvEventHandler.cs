using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using Agv = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Agv;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides handlers for agv related events.
/// </summary>
[UsedImplicitly]
public class AgvEventHandler :
    IConsumer<AgvNeedsCharging>,
    IConsumer<AgvBecameIdle>,
    IConsumer<AgvCanStartIdleAction>,
    IConsumer<ChargingAgvCanHandleActionsAgain>,
    IConsumer<AgvBatteryChargeChanged>,
    IConsumer<AgvMapReceived>,
    IConsumer<AgvErrorStatusReceived>,
    IConsumer<AgvStatusReceived>,
    IConsumer<AgvLastWillReceived>,
    IConsumer<AgvPreparedForStartingAction>,
    IConsumer<AgvLoadStatusReceived>,
    IConsumer<ParkingNodeSelectedForAgv>,
    IConsumer<QueueNodeSelectedForAgv>,
    IConsumer<ChargingNodeSelectedForAgv>,
    IConsumer<AgvModeChangePending>,
    IConsumer<AgvZonesReceived>,
    IConsumer<CriticalAgvHasNoCharger>,
    IConsumer<AgvBatteryChargedToMinimum>,
    IConsumer<SwitchChargingAgvsFailed>,
    IConsumer<AgvSetToPushOrJoystickMode>,
    IConsumer<AgvRemovedFromZones>,
    IConsumer<VehicleActionStatusChanged>
{
    private readonly ILogger<AgvEventHandler> _logger;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _semaphoreProvider;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IAgvDb _agvDb;
    private readonly IQueueRepository _queueRepository;
    private readonly IQueueManager _queueManager;
    private readonly ILoggingService _loggingService;

    /// <inheritdoc cref="AgvEventHandler"/>
    public AgvEventHandler(
        IActionGroupRepository actionGroupRepository,
        ISemaphoreProvider<IAgvsSemaphore> semaphoreProvider,
        IAgvDb agvDb,
        IChargingPointManager chargingPointManager,
        IQueueRepository queueRepository,
        IQueueManager queueManager,
        ILoggingService loggingService,
        ILogger<AgvEventHandler> logger)
    {
        _actionGroupRepository = actionGroupRepository;
        _semaphoreProvider = semaphoreProvider;
        _agvDb = agvDb;
        _chargingPointManager = chargingPointManager;
        _queueRepository = queueRepository;
        _queueManager = queueManager;
        _logger = logger;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Handles the <see cref="AgvNeedsCharging"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvNeedsCharging> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (!agv.IsAvailable(ignoreLocked: true))
        {
            // AGV is still busy
            _logger.LogDebug("AGV with CorrelationId {AgvCorrelationId} is not available ", agv.CorrelationId);
            return;
        }
        _logger.LogInformation("Trying to prepare AGV:{CorrelationId} for charging", agv.CorrelationId);
        await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (!agv.CanStartAutomaticAction || agv.ShouldStartManualAction)
            {
                _logger.LogDebug("AGV with CorrelationId {AgvCorrelationId} is in mode {Mode} and can not create automatic charge action", agvCorrelationId, agv.Mode.Type.ToString());
                return;
            }

            // Only start charging if we must or if there is nothing else to do
            IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agvCorrelationId);
            var actionGroup = actionGroups.FirstOrDefault(a => a.Actions.Any(ac => ac.ActionType == ActionType.Charge));
            if (actionGroup != null)
            {
                _logger.LogInformation("No need for another charge action because the agv {AgvCorrelationId} has already an assigned a charge action group", agv.CorrelationId);
                // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
                return;
            }

            var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
            bool hasNotStartedActionGroups = actionGroups.Any();
            bool agvMustCharge = agv.CheckMustCharge(agvSettings);
            _logger.LogInformation("AGV:{CorrelationId} must charge:{AgvMustCharge} & has not started action groups:{HasNotStartedActionGroups}",
                agv.CorrelationId,
                agvMustCharge,
                hasNotStartedActionGroups);
            if (agvMustCharge || !hasNotStartedActionGroups)
            {
                _logger.LogInformation("Prepare charging group for AGV:{CorrelationId}", agv.CorrelationId);
                await context.Publish(new PrepareChargingOfAgv(agv.CorrelationId));
            }
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="AgvBecameIdle"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBecameIdle> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (!agv.IsAvailable(ignoreLocked: true, ignoreError: true))
        {
            _logger.LogInformation("Agv with CorrelationId {AgvCorrelationId} is not available", agvCorrelationId);
            return;
        }

        bool hasUnfinishedActionGroup = await _actionGroupRepository.HasUnfinishedActionGroupAsync(agv.CorrelationId);

        bool agvModeAllowsStartingNextAction = !agv.HasPendingModeSwitch || agv is { HasPendingModeSwitch: true, Mode.CurrentActionGroupPolicy: CurrentActionGroupPolicy.FinishActionGroup };
        if (hasUnfinishedActionGroup && agvModeAllowsStartingNextAction)
        {
            _logger.LogDebug("Starting next action for agv: {AgvCorrelationId}", agv.CorrelationId);
            await context.Publish(new TryStartNextActionInActionGroup(agv.CorrelationId));
            return;
        }

        if (agv.HasPendingModeSwitch)
        {
            _logger.LogDebug("Do not try starting new action group for agv {AgvCorrelationId} until mode has been switched", agv.CorrelationId);
            return;
        }

        _logger.LogInformation("Try starting new action group for agv: {AgvCorrelationId}", agv.CorrelationId);
        await context.Publish(new TryStartNewActionGroup(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvCanStartIdleAction"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvCanStartIdleAction> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        await context.Publish(new PrepareIdleActionOfAgv(agvCorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="ChargingAgvCanHandleActionsAgain"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ChargingAgvCanHandleActionsAgain> context)
    {
        _logger.LogDebug("{MessageType} received", nameof(ChargingAgvCanHandleActionsAgain));
        await context.Publish(new TryReactivateChargingAgv(Guid.NewGuid()));
    }

    /// <summary>
    /// Handles the <see cref="AgvBatteryChargeChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBatteryChargeChanged> context)
    {
        var message = context.Message;
        var agvCorrelationId = message.CorrelationId;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);

        double oldSoc = message.OldBatteryCharge;
        double newSoc = message.NewBatteryCharge;

        bool isCharging = oldSoc < newSoc;
        bool wasMinChargedUp = oldSoc >= agvSettings.MinChargeUp;
        bool isMinChargedUp = newSoc >= agvSettings.MinChargeUp;

        bool wasMaxChargedUp = oldSoc >= agvSettings.MaxChargeUp;
        bool isMaxChargedUp = newSoc >= agvSettings.MaxChargeUp;

        bool isLow = newSoc <= agvSettings.BatteryLevelLow;

        if (!wasMinChargedUp && isMinChargedUp)
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} has reached min charge up level and can handle actions again", agvCorrelationId);

            if (isCharging)
            {
                await context.Publish(new AgvBatteryChargedToMinimum(agvCorrelationId));
            }
            await context.Publish(new ChargingAgvCanHandleActionsAgain(agvCorrelationId));
        }

        if (!wasMaxChargedUp && isMaxChargedUp && !agvSettings.InfiniteCharging)
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} has reached max charge up level. So aborting charge action now", agvCorrelationId);
            await context.Publish(new AbortChargeAction(message.CorrelationId));
        }

        if (!isCharging && isLow)
        {
            _logger.LogInformation("Agv:{CorrelationId} battery level is {NewSoc} and requires charging", agvCorrelationId, newSoc);
            await context.Publish(new AgvNeedsCharging(agvCorrelationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="AgvMapReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvMapReceived> context)
    {
        var map = context.Message.Map;

        // at the beginning the map is just related and empty.
        var agvGraph = await _agvDb.Graphs.GetAgvGraphAsync(context.Message.CorrelationId);

        // so take the existing nodes and edges from the agvGraph and persist it.
        await context.Publish(new SaveMap(map with { Nodes = agvGraph.Nodes, Edges = agvGraph.Edges }, context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvErrorStatusReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvErrorStatusReceived> context)
    {
        List<AgvError> agvErrors = context.Message.Errors;
        await context.Publish(new UpdateErrorStatus(context.Message.CorrelationId, agvErrors, context.Message.AgvPosition));
    }

    /// <summary>
    /// Handles the <see cref="AgvStatusReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStatusReceived> context)
    {
        await context.Publish(new UpdateAgvStatus(context.Message.CorrelationId, context.Message.SerialNumber, context.Message.AgvStatus));
    }

    /// <summary>
    /// Handles the <see cref="AgvPreparedForStartingAction"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPreparedForStartingAction> context)
    {
        await context.Publish(new StartActionOfActionGroup(context.Message.Action, context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvLastWillReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvLastWillReceived> context)
    {
        _logger.LogInformation("Agv with CorrelationId {AgvCorrelationId} sent last will", context.Message.CorrelationId);
        await context.Publish(new GoOffline(context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvLoadStatusReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvLoadStatusReceived> context)
    {
        await context.Publish(new UpdateCargo(context.Message.CorrelationId, context.Message.Cargos));
    }

    /// <summary>
    /// Handles the <see cref="ParkingNodeSelectedForAgv"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ParkingNodeSelectedForAgv> context)
    {
        await context.Publish(new CreateParkingActionGroup(Guid.NewGuid(), context.Message.CorrelationId, context.Message.ParkingNode));
    }

    /// <summary>
    /// Handles the <see cref="QueueNodeSelectedForAgv"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<QueueNodeSelectedForAgv> context)
    {
        await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), context.Message.CorrelationId, context.Message.QueueNode));
    }

    /// <summary>
    /// Handles the <see cref="ChargingNodeSelectedForAgv"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ChargingNodeSelectedForAgv> context)
    {
        var chargeStation = await _agvDb.Graphs.GetNodeAsync(context.Message.ChargeStationId);
        await context.Publish(new CreateChargingActionGroup(Guid.NewGuid(), context.Message.CorrelationId, chargeStation, context.Message.Initiator));
    }

    /// <summary>
    /// Handles the <see cref="AgvModeChangePending"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvModeChangePending> context)
    {
        var (agvCorrelationId, agvMode) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (agvMode.PendingType is ModeType.Normal)
        {
            return;
        }
        ICollection<ActionGroup> actionGroups = await _actionGroupRepository.GetPendingActionGroupsForAgvAsync(agvCorrelationId);

        ICollection<Guid> queueHeadAgvs = await _queueManager.GetAgvsAtQueueHeads();

        bool needsToBeRemovedFromQueue = agv.IsQueueing || queueHeadAgvs.Contains(agv.CorrelationId);

        if (agvMode is { PendingType: ModeType.Maintenance or ModeType.Locked } && needsToBeRemovedFromQueue)
        {
            await context.Publish(new RemoveAgvFromQueue(agvCorrelationId));
        }

        foreach (var actionGroup in actionGroups)
        {
            if (agvMode is { PendingType: ModeType.Maintenance or ModeType.Locked } && actionGroup.DoNotPause)
            {
                _logger.LogDebug("Cancelling/Aborting action group {CorrelationId} with do not pause flag during mode change to mode {Mode}", actionGroup.CorrelationId, agvMode.PendingType);
                await context.Publish(new CancelOrAbortActionGroup(actionGroup.CorrelationId));
                continue;
            }

            if (actionGroup.State == ActionGroupState.Created)
            {
                if (!actionGroup.IsManual && agvMode.AssignedActionGroupPolicy == AssignedActionGroupPolicy.Reset)
                {
                    _logger.LogDebug("Resetting action group {CorrelationId}", actionGroup.CorrelationId);
                    await context.Publish(new ResetActionGroup(actionGroup.CorrelationId, false));
                }

                if (actionGroup.IsManual && agvMode.CancelManualActions)
                {
                    _logger.LogDebug("Cancelling action group {CorrelationId}", actionGroup.CorrelationId);
                    await context.Publish(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.WebApi));
                }
                continue;
            }

            if (agvMode.CurrentActionGroupPolicy == CurrentActionGroupPolicy.Pause)
            {
                var pauseReason = agvMode.GetPauseReason();
                _logger.LogDebug("Pausing action group {CorrelationId} with reason {PauseReason}", actionGroup.CorrelationId, pauseReason);
                await context.Publish(new PauseActionGroup(actionGroup.CorrelationId, pauseReason));
            }
        }

        //If no running action group, switch now
        if (actionGroups.All(a => a.State != ActionGroupState.Started))
        {
            await context.Publish(new SwitchMode(agvCorrelationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="AgvZonesReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvZonesReceived> context)
    {
        await context.Publish(new ReceiveAgvZones(context.Message.CorrelationId, context.Message.SerialNumber, context.Message.AgvZones));
    }

    /// <summary>
    /// Handles the <see cref="CriticalAgvHasNoCharger"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<CriticalAgvHasNoCharger> context)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(context.Message.CorrelationId);
        try
        {
            var charger = await _chargingPointManager.ReserveChargerAsync(agv);
            await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, charger.Id));
        }
        catch (NoChargeStationAvailableException)
        {
            await HandleNoChargingStationAvailableAsync(context, agv);
        }
    }

    private async Task HandleNoChargingStationAvailableAsync(IPublishEndpoint context, Agv agv, Guid? excludedAgv = null)
    {
        ICollection<Agv> candidatesToStopCharging = (await _agvDb.Agvs.GetAgvsOnChargerWithAtLeastMinChargeUpLevel()).Where(c => excludedAgv == null || c.CorrelationId != excludedAgv).ToList();
        if (candidatesToStopCharging.Count == 0)
        {
            ICollection<Agv> agvs = await _agvDb.Agvs.GetAgvsOnChargerWithAtLeastChargeUpLevel(0);
            await _loggingService.LogBattery(_logger,
                LogLevel.Warning,
                LogCodes.BatteryNeedsChargingAllOccupied,
                agv.CorrelationId,
                null,
                "Agv cannot charge",
                $"Agv {agv.CorrelationId} needs charging but no chargers are available and no charging agv has at least min charge level. " +
                $"Agvs on charger: {string.Join(", ", agvs.Select(a => $"{a.CorrelationId} ({a.AgvStatus!.LastNodeId} | {a.AgvStatus!.BatteryCharge}%)"))}");

            // only switch if the other Agv is either not charging or already above the critical level to avoid further drawing switches.
            candidatesToStopCharging = agvs.Where(a => a.AgvStatus?.Charging ?? false).ToList();
        }

        var highestChargingAgv = candidatesToStopCharging.OrderBy(a => a.AgvStatus?.BatteryCharge ?? 0).Last();

        _logger.LogInformation("Agv {AgvCorrelationId} with critical battery level needs a charger. " +
                               "Switching with Agv {HighestChargingAgvCorrelationId} on charger (Charging: {IsCharging}, SOC: {BatteryCharge}%)",
            agv.CorrelationId,
            highestChargingAgv.CorrelationId,
            highestChargingAgv.AgvStatus?.Charging ?? false,
            highestChargingAgv.AgvStatus?.BatteryCharge ?? 0);
        await context.Publish(new SwitchChargingAgvs(agv.CorrelationId, highestChargingAgv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvBatteryChargedToMinimum"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBatteryChargedToMinimum> context)
    {
        ICollection<Agv> candidatesToStartCharging = await _agvDb.Agvs.GetNotChargingAgvsWithCriticalBatteryLevel();

        if (candidatesToStartCharging.Count == 0)
        {
            return;
        }

        var agv = await _agvDb.Agvs.GetAgvAsync(context.Message.CorrelationId);

        var agvToStartCharging = candidatesToStartCharging.OrderBy(a => a.AgvStatus?.BatteryCharge ?? double.MaxValue).First();

        await context.Publish(new SwitchChargingAgvs(agvToStartCharging.CorrelationId, agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="SwitchChargingAgvsFailed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<SwitchChargingAgvsFailed> context)
    {
        (var agvWithCriticalBatteryLevel, var chargingAgv, double retryDelaySec) = context.Message;

        await Task.Delay(TimeSpan.FromSeconds(retryDelaySec));
        var agv = await _agvDb.Agvs.GetAgvAsync(agvWithCriticalBatteryLevel);
        if (!agv.CanStartAutomaticAction || !agv.IsOnline)
        {
            _logger.LogWarning("Agv {AgvCorrelationId} can not start automatic actions in mode {AgvMode} or is offline ({IsAgvOffline}), not handling agv switch failure", agv.CorrelationId, agv.Mode, !agv.IsOnline);
            return;
        }
        try
        {
            var charger = await _chargingPointManager.ReserveChargerAsync(agv);
            await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, charger.Id));
        }
        catch (NoChargeStationAvailableException)
        {
            await HandleNoChargingStationAvailableAsync(context, agv, chargingAgv);
        }
    }

    /// <summary>
    /// Handles the <see cref="AgvSetToPushOrJoystickMode"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvSetToPushOrJoystickMode> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        if (agv.IsQueueing || await _queueRepository.GetQueueForAgv(agvCorrelationId) != null)
        {
            IReadOnlyList<AgvError> activeErrors = await _agvDb.Errors.GetActiveErrorsAsync(agvCorrelationId);
            bool hasQueueBlockingError = activeErrors.Any(e => e.IsQueueBlockingError());
            if (!hasQueueBlockingError)
            {
                var position = _agvDb.Positions.GetPosition(agvCorrelationId);
                _logger.LogInformation("Agv {AgvCorrelationId} moved manually out of queue", agvCorrelationId);
                await context.Publish(new AddSystemAgvErrors(agvCorrelationId, [new QueueBlockingError(agvCorrelationId, DateTime.UtcNow)], position.AgvPosition));
            }
        }
    }

    /// <summary>
    /// Handles the <see cref="AgvRemovedFromZones"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvRemovedFromZones> context)
    {
        await context.Publish(new RemoveAgvFromZones(context.Message.CorrelationId, context.Message.Zones));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<VehicleActionStatusChanged> context)
    {
        (var correlationId, var agvCorrelationId, string actionType, var actionStatus) = context.Message;
        var action = await _actionGroupRepository.GetActionByIdAsync(correlationId);
        if (action == null)
        {
            await _loggingService.LogAgv(_logger, 
                LogLevel.Information, 
                LogCodes.AgvSentInstantActionStatusChanged, 
                agvCorrelationId, 
                null, 
                "AGV Sent Instant Action Status Changed",
                $"AGV instant action {actionType} changed to status {actionStatus}",
                new Dictionary<string, object?>
                {
                    { LogLabels.ActionType, actionType },
                    { LogLabels.ActionStatus, actionStatus }
                });
            return;
        }

        await _loggingService.LogOrder(_logger,
            LogLevel.Information,
            LogCodes.AgvSentVehicleActionStatusChanged,
            agvCorrelationId,
            action.ParentId,
            "AGV Sent Action Status Changed",
            $"AGV action {actionType} changed to status {actionStatus}",
            new Dictionary<string, object?>
            {
                {LogLabels.ActionType, actionType},
                {LogLabels.ActionStatus, actionStatus}
            });
    }
}