using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Models.Settings;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.SemaphoreProvider;
using TransportOrder.Domain.Services.ActionGroup;
using TransportOrder.Messages.Commands.ActionGroup;
using TransportOrder.Messages.Commands.Agv;
using TransportOrder.Messages.Events.ActionGroup;
using TransportOrder.Messages.Events.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides handlers for agv related events.
/// </summary>
[UsedImplicitly]
public class AgvEventHandler : 
    IConsumer<AgvNeedsCharging>,
    IConsumer<AgvBecameIdle>,
    IConsumer<AgvCanStartIdleAction>,
    IConsumer<ChargingAgvsCanHandleActionsAgainEvent>,
    IConsumer<AgvBatteryChargeChanged>
{

    private readonly ILogger<AgvEventHandler> _logger;
    private readonly IAgvSagaCommunicationService _agvSagaCommunicationService;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IChargingService _chargingService;
    private readonly IActionManagerSemaphoreProvider _semaphoreProvider;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;

    /// <summary>
    /// Initializes a new <see cref="AgvEventHandler"/> instance.
    /// </summary>
    public AgvEventHandler(IAgvSagaCommunicationService agvSagaCommunicationService,
        IActionGroupRegistry actionGroupRegistry,
        IMaintenanceService maintenanceService,
        IChargingService chargingService,
        IActionManagerSemaphoreProvider semaphoreProvider,
        ISettingsProvider<AgvSettings> settingsProvider,
        ILogger<AgvEventHandler> logger)
    {
        _logger = logger;
        _agvSagaCommunicationService = agvSagaCommunicationService;
        _actionGroupRegistry = actionGroupRegistry;
        _maintenanceService = maintenanceService;
        _chargingService = chargingService;
        _semaphoreProvider = semaphoreProvider;
        _settingsProvider = settingsProvider;
    }

    /// <summary>
    /// Handles the <see cref="AgvNeedsCharging"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvNeedsCharging> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var cancellationToken = context.CancellationToken;
        _logger.LogDebug("{MessageType} for Agv: {AgvCorrelationId} received", nameof(AgvNeedsCharging), agvCorrelationId);

        if (!await _agvSagaCommunicationService.IsAgvReadyForBusyAsync(agvCorrelationId, cancellationToken, ignoreCharging: false, ignoreLocked: true))
        {
            // Agv might be charging
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy", agvCorrelationId);
            return;
        }
        _logger.LogDebug("Assigning action for Agv with CorrelationId: {AgvCorrelationId} because it is ready", agvCorrelationId);
        var agv = await _agvSagaCommunicationService.GetAgvAsync(agvCorrelationId, cancellationToken);
        await PublishPendingActions(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="AgvBecameIdle"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBecameIdle> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var cancellationToken = context.CancellationToken;
        _logger.LogDebug("{MessageType} for Agv: {AgvCorrelationId} received", nameof(AgvBecameIdle), agvCorrelationId);
        if (!await _agvSagaCommunicationService.IsAgvReadyForBusyAsync(agvCorrelationId, cancellationToken, ignoreCharging: false, ignoreLocked: true))
        {
            // Agv might be charging
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy", agvCorrelationId);
            return;
        }
        _logger.LogDebug("Assigning action for Agv with CorrelationId: {AgvCorrelationId} because it is ready", agvCorrelationId);
        var agv = await _agvSagaCommunicationService.GetAgvAsync(agvCorrelationId, cancellationToken);
        await PublishPendingActions(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="AgvCanStartIdleAction"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvCanStartIdleAction> context)
    {
        var agv = context.Message.Agv;

        if (_chargingService.AgvIsEligibleForCharging(agv))
        {
            await context.Publish(new CreateChargingActionGroup(agv));
            return;
        }

        await context.Publish(new CreateParkingActionGroup(agv));
    }

    /// <summary>
    /// Handles the <see cref="ChargingAgvsCanHandleActionsAgainEvent"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ChargingAgvsCanHandleActionsAgainEvent> context)
    {
        _logger.LogDebug("{MessageType} received", nameof(ChargingAgvsCanHandleActionsAgainEvent));
        await context.Publish(new TryReactivateChargingAgv());
    }

    private async Task PublishPendingActions(ConsumeContext context, IAgv agv)
    {
        await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {

            if (agv.IsLocked)
            {
                await context.Publish(new TryStartManualAction(agv));
                return;
            }

            bool needsCharging = _chargingService.AgvNeedsCharging(agv);
            bool isCarryingLoad = await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId);
            bool needsMaintenance = _maintenanceService.MaintenanceRequested(agv);

            bool shouldCharge = !isCarryingLoad && needsCharging;
            bool shouldDoMaintenance = !isCarryingLoad && needsMaintenance;

            if (shouldCharge)
            {
                await context.Publish(new CreateChargingActionGroup(agv));
                return;
            }

            if (shouldDoMaintenance)
            {
                await context.Publish(new CreateMaintenanceActionGroup(agv));
                return;
            }

            if (isCarryingLoad)
            {
                await context.Publish(new TryStartNextActionInActionGroup(agv));
                return;
            }

            await context.Publish(new TryStartNewActionGroup(agv));
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="AgvBatteryChargeChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBatteryChargeChanged> context)
    {
        var agvSettings = _settingsProvider.GetSettings();

        double oldSoc = context.Message.OldBatteryCharge;
        double newSoc = context.Message.NewBatteryCharge;

        bool isCharging = oldSoc < newSoc;
        bool wasGood = oldSoc > agvSettings.BatteryLevelLow;
        bool isGood = newSoc > agvSettings.BatteryLevelLow;

        // Check if AGV is charging and no longer low on battery
        if (!wasGood && isGood)
        {
            await context.Publish(new ChargingAgvsCanHandleActionsAgainEvent());
        }

        // Battery needs charging
        if (!isCharging && !isGood)
        {
            await context.Publish(new AgvNeedsCharging(context.Message.CorrelationId));
        }
    }
}