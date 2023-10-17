using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
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
    IConsumer<ChargingAgvsCanHandleActionsAgainEvent>,
    IConsumer<AgvBatteryChargeChanged>,
    IConsumer<AgvMapReceived>,
    IConsumer<AgvErrorStatusReceived>,
    IConsumer<AgvStatusReceived>,
    IConsumer<AgvStartedCharging>,
    IConsumer<AgvStoppedCharging>,
    IConsumer<AgvGraphChanged>,
    IConsumer<AgvWentOffline>, 
    IConsumer<AgvPreparedForStartingAction>
{

    private readonly ILogger<AgvEventHandler> _logger;
    private readonly IAgvRepository _agvRepository;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly IActionManagerSemaphoreProvider _semaphoreProvider;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;

    /// <summary>
    /// Initializes a new <see cref="AgvEventHandler"/> instance.
    /// </summary>
    public AgvEventHandler(
        IAgvRepository agvRepository,
        IActionGroupRegistry actionGroupRegistry,
        IActionManagerSemaphoreProvider semaphoreProvider,
        ISettingsProvider<AgvSettings> settingsProvider,
        ILogger<AgvEventHandler> logger)
    {
        _logger = logger;
        _agvRepository = agvRepository;
        _actionGroupRegistry = actionGroupRegistry;
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
 
        if (!agv.IsAvailable( ignoreCharging: false, ignoreLocked: true))
        {
            // Agv might be charging
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not available.", agvCorrelationId);
            return;
        }
        _logger.LogDebug("Assigning action for Agv with CorrelationId: {AgvCorrelationId} because it is ready", agvCorrelationId);
        await PublishPendingActions(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="AgvBecameIdle"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBecameIdle> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for Agv: {AgvCorrelationId} received", nameof(AgvBecameIdle), agvCorrelationId);
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
 
        if (!agv.IsAvailable( ignoreCharging: false, ignoreLocked: true))
        {
            // Agv might be charging
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not available.", agvCorrelationId);
            return;
        }
        _logger.LogDebug("Assigning action for Agv with CorrelationId: {AgvCorrelationId} because it is ready", agvCorrelationId);
        await PublishPendingActions(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="AgvCanStartIdleAction"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvCanStartIdleAction> context)
    {
        var agv = context.Message.Agv;
        var agvSettings = _settingsProvider.GetSettings();
        if (agv.CheckIsEligibleForCharging(agvSettings))
        {
            await context.Publish(new CreateChargingActionGroup(agv.CorrelationId));
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
        List<AgvError>? errors = context.Message.Errors;
        List<AgvError> agvErrors = new();
        if (errors != null)
        {
            agvErrors = errors;
        }

        await context.Publish(new UpdateErrorStatus(context.Message.CorrelationId, agvErrors));
    }

    /// <summary>
    /// Handles the <see cref="AgvStatusReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStatusReceived> context)
    {
        _logger.LogDebug("Received status update for {CorrelationId}", context.Message.CorrelationId);
        try
        {
            await _agvRepository.GetAgvAsync(context.Message.CorrelationId);
            await context.Publish(new UpdateAgvStatus(context.Message.CorrelationId, context.Message.AgvId, context.Message.AgvStatus));
        }
        catch (AgvNotFoundException)
        {
            await context.Publish(new CreateAgv(context.Message.CorrelationId, context.Message.AgvId, context.Message.AgvStatus));
        }
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
        await context.Publish(new StartAction(context.Message.Action, context.Message.AssignedAgv));
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
            var agvSettings = _settingsProvider.GetSettings();
            bool needsCharging = agv.CheckNeedsCharging(agvSettings);
            bool isCarryingLoad = await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId);

            bool shouldCharge = !isCarryingLoad && needsCharging;
            bool shouldDoMaintenance = !isCarryingLoad && agv.NeedsMaintenance;

            if (shouldCharge)
            {
                await context.Publish(new CreateChargingActionGroup(agv.CorrelationId));
                return;
            }

            if (shouldDoMaintenance)
            {
                await context.Publish(new CreateMaintenanceActionGroup(agv.CorrelationId));
                return;
            }

            if (isCarryingLoad)
            {
                await context.Publish(new TryStartNextActionInActionGroup(agv.CorrelationId));
                return;
            }

            await context.Publish(new TryStartNewActionGroup(agv.CorrelationId));
        }, context.CancellationToken);
    }
}