using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

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
    IConsumer<AgvStartedCharging>,
    IConsumer<AgvStoppedCharging>,
    IConsumer<AgvGraphChanged>,
    IConsumer<AgvWentOffline>,
    IConsumer<AgvPreparedForStartingAction>,
    IConsumer<AgvLoadStatusReceived>
{
    private readonly ILogger<AgvEventHandler> _logger;
    private readonly IAgvRepository _agvRepository;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IActionManagerSemaphoreProvider _semaphoreProvider;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;

    /// <summary>
    /// Initializes a new <see cref="AgvEventHandler"/> instance.
    /// </summary>
    public AgvEventHandler(
        IAgvRepository agvRepository,
        IActionGroupRepository actionGroupRepository,
        IActionManagerSemaphoreProvider semaphoreProvider,
        ISettingsProvider<AgvSettings> settingsProvider,
        ILogger<AgvEventHandler> logger)
    {
        _logger = logger;
       _agvRepository = agvRepository;
        _actionGroupRepository = actionGroupRepository;
        _semaphoreProvider = semaphoreProvider;
        _settingsProvider = settingsProvider;
    }

    /// <summary>
    /// Handles the <see cref="AgvNeedsCharging"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvNeedsCharging> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for Agv: {AgvCorrelationId} received", nameof(AgvNeedsCharging), agvCorrelationId);

        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);

        if (!agv.IsAvailable(ignoreCharging: false, ignoreLocked: true))
        {
            // AGV is still busy
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not available (charging ignored).", agvCorrelationId);
            return;
        }
        _logger.LogDebug("Try assigning charging action for agv with CorrelationId: {AgvCorrelationId} because it is ready (and needs charging).", agvCorrelationId);
        await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            bool hasUnfinishedDropAction = await _actionGroupRepository.HasUnfinishedDropActionsAsync(agv.CorrelationId);
            var agvSettings = _settingsProvider.GetSettings();
            
            if (agv.IsLocked)
            {
                _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is locked. Try start manual action.", agvCorrelationId);
                await context.Publish(new TryStartManualAction(agv.CorrelationId));
                return;
            }
            if (hasUnfinishedDropAction)
            {
                _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} has unfinished drop action.", agvCorrelationId);
                return;
            }

            // Only start charging if we must or if there is nothing else to do
            IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> notStartedActionGroups = await _actionGroupRepository.GetNotStartedActionGroupsAsync(agv.CorrelationId);

            if (agv.CheckMustCharge(agvSettings) || !notStartedActionGroups.Any())
            {
                _logger.LogInformation("Create and start charging group for agv {CorrelationId}.", agv.CorrelationId);
                await context.Publish(new StartChargingActionGroup(agv.CorrelationId));
            }
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="AgvBecameIdle"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBecameIdle> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for Agv: {AgvCorrelationId} received", nameof(AgvBecameIdle), agvCorrelationId);
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);

        if (!agv.IsAvailable(ignoreCharging: false, ignoreLocked: true))
        {
            // Agv might be charging
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not available.", agvCorrelationId);
            return;
        }
        _logger.LogDebug("Assigning pending action for agv with CorrelationId: {AgvCorrelationId} because it is ready (became idle).", agvCorrelationId);
        await PublishPendingActions(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="AgvCanStartIdleAction"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvCanStartIdleAction> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
        var agvSettings = _settingsProvider.GetSettings();
        if (agv.CheckShouldCharge(agvSettings))
        {
            await context.Publish(new StartChargingActionGroup(agv.CorrelationId));
            return;
        }

        await context.Publish(new CreateParkingActionGroup(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="ChargingAgvCanHandleActionsAgain"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ChargingAgvCanHandleActionsAgain> context)
    {
        _logger.LogDebug("{MessageType} received", nameof(ChargingAgvCanHandleActionsAgain));
        await context.Publish(new TryReactivateChargingAgv());
    }

    /// <summary>
    /// Handles the <see cref="AgvBatteryChargeChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBatteryChargeChanged> context)
    {
        var agvSettings = _settingsProvider.GetSettings();
        var agvCorrelationId = context.Message.CorrelationId;

        double oldSoc = context.Message.OldBatteryCharge;
        double newSoc = context.Message.NewBatteryCharge;

        bool isCharging = oldSoc < newSoc;
        bool wasGood = oldSoc > agvSettings.BatteryLevelLow;
        bool isGood = newSoc > agvSettings.BatteryLevelLow;

        // Check if AGV is charging and no longer low on battery
        if (!wasGood && isGood)
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is no longer low on battery and can handle actions again.", agvCorrelationId);
            await context.Publish(new ChargingAgvCanHandleActionsAgain(agvCorrelationId));
        }

        // Battery needs charging
        if (!isCharging && !isGood)
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} needs charging.", agvCorrelationId);
            await context.Publish(new AgvNeedsCharging(agvCorrelationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="AgvMapReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvMapReceived> context)
    {
        var map = context.Message.AgvMap;
        await context.Publish(new SaveMap(map, map.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvErrorStatusReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvErrorStatusReceived> context)
    {
        List<AgvError> agvErrors = context.Message.Errors ?? new List<AgvError>();

        await context.Publish(new UpdateErrorStatus(context.Message.CorrelationId, agvErrors));
    }

    /// <summary>
    /// Handles the <see cref="AgvStatusReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStatusReceived> context)
    {
        await context.Publish(new UpdateAgvStatus(context.Message.CorrelationId, context.Message.AgvId, context.Message.AgvStatus));
    }

    /// <summary>
    /// Handles the <see cref="AgvStartedCharging"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStartedCharging> context)
    {
        await context.Publish(new StartCharging(context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvStoppedCharging"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStoppedCharging> context)
    {
        await context.Publish(new StopCharging(context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvGraphChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvGraphChanged> context)
    {
        _logger.LogInformation("{MessageType} {CorrelationId}", nameof(AgvGraphChanged), context.Message.CorrelationId);
        await context.Publish(new UpdateGraph(context.Message.CorrelationId, context.Message.AgvGraph));
    }

    /// <summary>
    /// Handles the <see cref="AgvPreparedForStartingAction"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPreparedForStartingAction> context)
    {
        await context.Publish(new StartAction(context.Message.Action, context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="AgvWentOffline"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvWentOffline> context)
    {
        await context.Publish(new GoOffline(context.Message.CorrelationId, context.GetSchedulingTokenId()!.Value));
    }

    private async Task PublishPendingActions(ConsumeContext context, Domain.Models.Agv.Agv agv)
    {
        await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (agv.IsLocked)
            {
                await context.Publish(new TryStartManualAction(agv.CorrelationId));
                return;
            }

            bool isCarryingLoad = await _actionGroupRepository.HasUnfinishedDropActionsAsync(agv.CorrelationId);
            bool hasUnfinishedActionGroup = await _actionGroupRepository.HasUnfinishedActionGroupAsync(agv.CorrelationId);
            bool shouldDoMaintenance = !isCarryingLoad && agv.NeedsMaintenance;

            if (shouldDoMaintenance)
            {
                await context.Publish(new CreateMaintenanceActionGroup(agv.CorrelationId));
                return;
            }

            if (hasUnfinishedActionGroup)
            {
                _logger.LogDebug("Starting next action for agv: {AgvCorrelationId}.", agv.CorrelationId);
                await context.Publish(new TryStartNextActionInActionGroup(agv.CorrelationId));
                return;
            }

            _logger.LogDebug("Starting new action group for agv: {AgvCorrelationId}.", agv.CorrelationId);
            await context.Publish(new TryStartNewActionGroup(agv.CorrelationId));
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="AgvLoadStatusReceived"/> event.
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvLoadStatusReceived> context)
    {
        await context.Publish(new UpdateCargo(context.Message.CorrelationId, context.Message.Cargos));
    }
}