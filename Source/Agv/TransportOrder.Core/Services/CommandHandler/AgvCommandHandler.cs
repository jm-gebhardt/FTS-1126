using AutoMapper;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;
using System.Collections.ObjectModel;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using AgvSettings = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvSettings;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvGraph;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides a handler for agv related commands.
/// </summary>
[UsedImplicitly]
public class AgvCommandHandler :
    IConsumer<TryReactivateChargingAgv>,
    IConsumer<UpdateErrorStatus>,
    IConsumer<AbortChargeAction>,
    IConsumer<UpdateAgvStatus>,
    IConsumer<SetAgvGraphExist>,
    IConsumer<GoOffline>,
    IConsumer<UpdateAgvData>,
    IConsumer<LockAgv>,
    IConsumer<SetUserMode>,
    IConsumer<SetNormalMode>,
    IConsumer<SetMaintenanceMode>,
    IConsumer<UnlockAgv>,
    IConsumer<AcceptAgvCommand>,
    IConsumer<SetAgvForceIdle>,
    IConsumer<HandleAgvActionStartFailure>,
    IConsumer<HandleActionCompletion>,
    IConsumer<PrepareAgvForStartingAction>,
    IConsumer<UpdateCargo>,
    IConsumer<ResetOfflineWatchdog>,
    IConsumer<PrepareParkingOfAgv>,
    IConsumer<PrepareChargingOfAgv>,
    IConsumer<UpdateAgvSettingsGroupReference>
{
    private readonly ILogger<AgvCommandHandler> _logger;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _agvsSemaphore;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IMapper _mapper;
    private readonly ProductionOptions _productionOptions;
    private readonly IAgvDb _agvDb;
    private readonly IParkPointService _parkPointService;
    private readonly INodeStateRepository _nodeStateRepository;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IMetricCollectionService _metricCollectionService;
    private readonly IStationRepository _stationRepository;

    /// <inheritdoc cref="AgvCommandHandler"/>
    public AgvCommandHandler(ISemaphoreProvider<IAgvsSemaphore> agvsSemaphore,
        IAgvDb agvDb,
        IActionGroupRepository actionGroupRepository,
        IParkPointService parkPointService,
        IChargingPointManager chargingPointManager,
        INodeStateRepository nodeStateRepository,
        IMetricCollectionService metricCollectionService,
        IMapper mapper,
        ProductionOptions productionOptions,
        IStationRepository stationRepository,
        ILogger<AgvCommandHandler> logger
    )
    {
        _logger = logger;
        _agvsSemaphore = agvsSemaphore;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _parkPointService = parkPointService;
        _nodeStateRepository = nodeStateRepository;
        _mapper = mapper;
        _metricCollectionService = metricCollectionService;
        _productionOptions = productionOptions;
        _chargingPointManager = chargingPointManager;
        _stationRepository = stationRepository;
    }

    /// <summary>
    /// Handles the <see cref="PrepareChargingOfAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PrepareChargingOfAgv> context)
    {
        var (agvCorrelationId, initiator) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        bool hasUnfinishedActionGroup = await _actionGroupRepository.HasUnfinishedActionGroupAsync(agv.CorrelationId);
        _logger.LogDebug("Evaluating {Name}, has unfinished action: {HasUnfinishedActionGroup} for agv {AgvCorrelationId}.", nameof(PrepareChargingOfAgv), hasUnfinishedActionGroup, agv.CorrelationId);

        if (agv.IsLocked || hasUnfinishedActionGroup)
        {
            return;
        }

        if (agv.IsCharging || await _chargingPointManager.AgvHasReservedChargerAsync(agvCorrelationId))
        {
            _logger.LogDebug("No need for another charge action because the agv {SerialNumber}/{AgvCorrelationId} is already charging", agv.SerialNumber, agv.CorrelationId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        // Let's see if there is a pending charge action group
        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> actionGroups = await _actionGroupRepository.GetActionGroupsAsync(new[] { ActionGroupState.Created }, assignedAgvCorrelationId: agvCorrelationId);
        var actionGroup = actionGroups.FirstOrDefault(a => a.Actions.Any(ac => ac.ActionType == ActionType.Charge));
        if (actionGroup != null)
        {
            _logger.LogDebug("No need for another charge action because the agv {SerialNumber}/{AgvCorrelationId} has already an assigned a charge action group", agv.SerialNumber, agv.CorrelationId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        var chargingNode = await _chargingPointManager.ReserveChargerAsync(agv);
        await PublishNodeStateChange(context, chargingNode.Id);
        await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, chargingNode.Id, initiator));
    }

    /// <summary>
    /// Handles the <see cref="PrepareParkingOfAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PrepareParkingOfAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);

        if (agv.CheckShouldCharge(agvSettings) || agv.IsLocked)
        {
            return;
        }
        if (await _parkPointService.IsOnAParkNodeAsync(agv) || await _parkPointService.AgvHasReservedParkingNodeAsync(agvCorrelationId))
        {
            _logger.LogDebug("No need for a park action because the agv is already in parking for agvId: {AgvCorrelationId}.", agv.CorrelationId);
            return;
        }
        _logger.LogInformation("Starting park action for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);

        var parkNode = await _parkPointService.ReserveParkAsync(agv);
        await PublishNodeStateChange(context, parkNode.Id);
        await context.Publish(new ParkingNodeSelectedForAgv(agv.CorrelationId, parkNode.Id));
    }

    /// <summary>
    /// Handles the <see cref="TryReactivateChargingAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryReactivateChargingAgv> context)
    {
        var cancellationToken = context.CancellationToken;
        await _agvsSemaphore.ExecuteLockedAsync(async () =>
        {
            ICollection<Domain.Models.Agv.Agv> goodAgvs = await _agvDb.Agvs.GetChargingAgvsWithMinBatteryLevelAsync();

            // preliminary logic: just go through all potential available Agvs and try to start a new action
            foreach (var agv in goodAgvs.OrderByDescending(a => a.AgvStatus!.BatteryCharge))
            {
                if (!await _actionGroupRepository.AgvHasPendingActions(agv.CorrelationId))
                {
                    _logger.LogDebug("No action needed for charging Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
                    continue;
                }

                var action = await _actionGroupRepository.GetCurrentAgvActionAsync(agv.CorrelationId);
                if (action == null)
                {
                    _logger.LogDebug("Agv is not charging at all with CorrelationId: {AgvCorrelationId}. So no need to abort it in order to get it to work again.", agv.CorrelationId);
                    continue;
                }

                _logger.LogDebug("There is a new action for the charging Agv with CorrelationId: {AgvCorrelationId}. Abort charging action group: {ActionGroupCorrelationId}", agv.CorrelationId, action.ParentId);
                await context.Publish(new AbortActionGroup(action.ParentId, AbortReason.ReactivatedFromCharging), cancellationToken);

                // only do this for the Agv with the best battery level and quit
                return;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="UpdateErrorStatus"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateErrorStatus> context)
    {
        (var correlationId, List<AgvError> errors, _) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        bool hadError = agv.HasError;
        bool agvHasError = errors.HasAnyError();

        foreach (var agvError in errors.Where(e => e.ErrorLevel == ErrorLevel.Fatal))
        {
            if (agvError.ErrorFlag == ErrorFlag.Battery)
            {
                _logger.LogBatteryError(LogCodes.BatteryError, agv.CorrelationId, agvError.ErrorCode, "Battery Error", $"Battery of AGV {agv.SerialNumber} threw error {agvError.ErrorDescription}");
                continue;
            }
            _logger.LogAgvError(LogCodes.AgvError, agv.CorrelationId, agvError.ErrorCode, "AGV Error", $"AGV {agv.SerialNumber} threw error {agvError.ErrorDescription}");
        }
        foreach (var agvError in errors.Where(e => e.ErrorLevel == ErrorLevel.Warning))
        {
            if (agvError.ErrorFlag == ErrorFlag.Battery)
            {
                _logger.LogBatteryWarning(LogCodes.BatteryWarning, agv.CorrelationId, agvError.ErrorCode, "Battery Warning", $"Battery of AGV {agv.SerialNumber} threw warning {agvError.ErrorDescription}");
                continue;
            }
            _logger.LogAgvWarning(LogCodes.AgvWarning, agv.CorrelationId, agvError.ErrorCode, "AGV Warning", $"AGV {agv.SerialNumber} threw warning {agvError.ErrorDescription}");
        }

        if (agvHasError != hadError)
        {
            agv.HasError = agvHasError;
            await _agvDb.Agvs.UpdateAgvAsync(agv);
        }

        if (hadError && !agvHasError && agv.AgvStatus?.Busy == false)
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="AbortChargeAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AbortChargeAction> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var action = await _actionGroupRepository.GetCurrentAgvActionAsync(agvCorrelationId);
        if (action?.ActionType == ActionType.Charge)
        {
            _logger.LogInformation("Aborting charging action group: {ActionGroupCorrelationId}", action.ParentId);
            await context.Publish(new AbortActionGroup(action.ParentId, AbortReason.ReactivatedFromCharging), context.CancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvStatus"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvStatus> context)
    {
        (var correlationId, string _, var agvStatusMessageContent) = context.Message;
        var agvStatus = _mapper.Map<AgvStatus>(agvStatusMessageContent);

        var agv = await _agvDb.Agvs.TryGetAgvAsync(correlationId);
        if (agv == null)
        {
            await CreateAgv(context);
            return;
        }

        _logger.LogDebug("Updating AgvStatus for agv {AgvCorrelationId}, {OldStatus} -> {NewStatus}", correlationId, agv.AgvStatus?.ToString(), agvStatus.ToString());
        var oldStatus = agv.AgvStatus;
        bool wasOnline = agv.IsOnline;
       
        if (oldStatus != null && agv.GraphExists)
        {
            await ResetLastNodeId(oldStatus, agvStatus);
        }
        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        await ResetOfflineWatchDog(context, agv, agvSettings);
        bool wasReady = agv.IsReady;
        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(agvStatus);
        bool isReady = agv.IsReady;
        
        LogBatteryStates(oldStatus, agv.AgvStatus, agvSettings);
        _metricCollectionService.CollectBatteryChargeMetrics(agvStatus);

        if (!wasReady && isReady)
        {
            _logger.LogAgvInformation(LogCodes.AgvReady, correlationId, "AGV Ready", $"AGV {agv.SerialNumber} became ready");
        }

        // Only request AgvGraph if a global Graph exists
        if (agv.ShouldRequestGraph())
        {
            if (await _agvDb.Graphs.ExistsAsync())
            {
                await PublishGraphRequestAsync(context, agv);
            }
            else
            {
                await context.Publish(new GraphMissing(agv.CorrelationId));
            }
        }

        if (agv.ShouldRequestData())
        {
            await PublishDataRequestAsync(context, agv);
        }

        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(UpdateAgvStatus), agv.CorrelationId);
            return;
        }
        await PublishAgvStatusUpdateEvents(context, agv, batteryChargeChanged, oldStatus, lastNodeIdChanged, wasOnline, agvSettings);

        await TryAutoAccept(context, agv, context.CancellationToken);
    }

    private async Task ResetOfflineWatchDog(ConsumeContext context, Domain.Models.Agv.Agv agv, AgvSettingsReadModel agvSettings)
    {
        if (agv.AgvOfflineTokenId != null)
        {
            var scheduler = context.GetPayload<MessageSchedulerContext>();
            await scheduler.CancelScheduledPublish<AgvWentOffline>(agv.AgvOfflineTokenId.Value);
        }

        int delay = agv.CurrentAgvState == AgvState.Busy ? agvSettings.AgvOfflineTimeoutMsWhileBusy : agvSettings.AgvOfflineTimeoutMsWhileIdle;
        var creationTimeStamp = DateTime.UtcNow;
        ScheduledMessage<AgvWentOffline> delayedMessage = await context.SchedulePublish(creationTimeStamp.AddMilliseconds(delay), new AgvWentOffline(agv.CorrelationId, creationTimeStamp));
        agv.SetOnline(delayedMessage.TokenId);
    }

    private async Task PublishAgvStatusUpdateEvents(IPublishEndpoint context,
        Domain.Models.Agv.Agv agv,
        bool batteryChargeChanged,
        AgvStatus? oldStatus,
        bool lastNodeIdChanged,
        bool wasOnline,
        AgvSettingsReadModel agvSettings)
    {
        var agvStatus = agv.AgvStatus!;
        if (batteryChargeChanged)
        {
            var agvBatteryStatus =
                new AgvBatteryStatus(
                    _mapper.Map<Gebhardt.Acc.Common.Domain.Models.AgvStatus>(agvStatus),
                    _mapper.Map<AgvSettings>(
                        agvSettings));

            await context.Publish(new AgvBatteryChargeChanged(oldStatus!.BatteryCharge, agvBatteryStatus));
        }
 
        if (lastNodeIdChanged)
        {
            await context.Publish(new AgvLastNodeIdChanged(agv.CorrelationId, oldStatus!.LastNodeId, agvStatus.LastNodeId));
        }
        bool isIdle = agv.CurrentAgvState == AgvState.Idle && !agvStatus.Busy;
        if (isIdle && oldStatus!.Busy)
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        }
        if (!wasOnline && agv.IsOnline)
        {
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, true));
            _logger.LogAgvInformation(LogCodes.AgvOnline, agvStatus.CorrelationId, "AGV Online", $"AGV {agv.SerialNumber} is online");

            if (isIdle)
            {
                await context.Publish(new AgvBecameIdle(agv.CorrelationId));
            }
        }
    }

    /// <summary>
    /// Handles the <see cref="HandleActionCompletion"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleActionCompletion> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been completed and type is not charge", correlationId);

        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        await TransitionToIdleAsync(context, agv, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="SetAgvGraphExist"/>
    /// </summary>
    public async Task Consume(ConsumeContext<SetAgvGraphExist> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);

        agv.GraphExists = true;

        _logger.LogAgvInformation(LogCodes.AgvGraphReceived, correlationId, "AGV Graph Received", $"AGV {agv.SerialNumber} graph has been received");
        await _agvDb.Agvs.UpdateAgvAsync(agv);
    }

    /// <summary>
    /// Handles the <see cref="GoOffline"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<GoOffline> context)
    {
        var (correlationId, offlineTokenId) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);

        bool wasOnline = agv.IsOnline;
        agv.GoOffline(offlineTokenId);
        if (wasOnline && !agv.IsOnline)
        {
            if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
            {
                _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(GoOffline), agv.CorrelationId);
                return;
            }
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, false));
            _logger.LogAgvInformation(LogCodes.AgvOffline, agv.CorrelationId, "AGV Offline", $"AGV {agv.SerialNumber} is offline");
        }
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvData"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvData> context)
    {
        var (correlationId, agvData) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        await _agvDb.Data.AddOrUpdateAsync(agvData);
        agv.DataExist = true;
        var settingsGroup = await _agvDb.AgvSettings.GetSettingsGroupForAgv(agv);
        agv.AgvSettingsGroup = settingsGroup.Id;
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(UpdateAgvData), agv.CorrelationId);
        }
        _logger.LogAgvInformation(LogCodes.AgvInformationReceived, correlationId, "AGV Information Received", $"AGV {agv.SerialNumber} information has been received");
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvSettingsGroupReference"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvSettingsGroupReference> context)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(context.Message.CorrelationId);
        var settingsGroup = await _agvDb.AgvSettings.GetSettingsGroupForAgv(agv);
        agv.AgvSettingsGroup = settingsGroup.Id;
        await _agvDb.Agvs.UpdateAgvAsync(agv);
    }

    /// <summary>
    /// Handles the <see cref="SetUserMode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetUserMode> context)
    {
        (var agvCorrelationId, string? setter) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        agv.SetUserMode(setter);

        await _agvDb.Agvs.UpdateAgvAsync(agv);
        _logger.LogAgvInformation(LogCodes.AgvModeChanged, agv.CorrelationId, "Agv mode changed", $"AGV { agv.SerialNumber} set to mode '{agv.Mode.Type.ToString()}'");
        await context.Publish(new AgvModeChanged(agvCorrelationId, agv.Mode));
    }

    /// <summary>
    /// Handles the <see cref="SetNormalMode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetNormalMode> context)
    {
        (var agvCorrelationId, string? setter) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        agv.SetNormalMode(setter);

         await _agvDb.Agvs.UpdateAgvAsync(agv);
        _logger.LogAgvInformation(LogCodes.AgvModeChanged, agv.CorrelationId, "Agv mode changed", $"AGV {agv.SerialNumber} set to mode '{agv.Mode.Type.ToString()}'");

        await context.Publish(new AgvModeChanged(agvCorrelationId, agv.Mode));
        await context.Publish(new AgvBecameIdle(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="SetMaintenanceMode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetMaintenanceMode> context)
    {
        (var agvCorrelationId, string? setter, string? reason, bool cancelManualActions) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        agv.SetMaintenanceMode(reason, setter);

        await _agvDb.Agvs.UpdateAgvAsync(agv);

        _logger.LogAgvInformation(LogCodes.AgvModeChanged, agv.CorrelationId, "Agv mode changed", $"AGV {agv.SerialNumber} set to mode '{agv.Mode.Type.ToString()}'");
        await context.Publish(new AgvModeChanged(agvCorrelationId, agv.Mode, cancelManualActions));
    }

    /// <summary>
    /// Handles the <see cref="LockAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<LockAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await TryGetAgv(agvCorrelationId);
        if (agv == null)
        {
            await context.RespondAsync(new AgvNotFoundResult(agvCorrelationId));
            return;
        }
        agv.Lock(context.Message.Reason, context.Message.Setter);
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(LockAgv), agv.CorrelationId);
        }
        _logger.LogAgvInformation(LogCodes.AgvLocked, agv.CorrelationId, "AGV Locked", $"AGV {agv.SerialNumber} is locked");
        await context.Publish(new AgvLockedChanged(agv.CorrelationId, true));
        await context.RespondAsync(new LockAgvResult(agv.CorrelationId));
        await context.Publish(new AgvModeChanged(agvCorrelationId, agv.Mode));
    }

    /// <summary>
    /// Handles the <see cref="UnlockAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UnlockAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await TryGetAgv(agvCorrelationId);
        if (agv == null)
        {
            await context.RespondAsync(new AgvNotFoundResult(agvCorrelationId));
            return;
        }
        agv.Unlock();

        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(UnlockAgv), agv.CorrelationId);
        }
        _logger.LogAgvInformation(LogCodes.AgvUnlocked, agv.CorrelationId, "AGV Unlocked", $"AGV {agv.SerialNumber} is unlocked");

        // publish also when state was already idle, to trigger further assignments
        _logger.LogInformation("Agv {correlationId} was unlocked.", agvCorrelationId);
        await context.Publish(new AgvBecameIdle(agv.CorrelationId), context.CancellationToken);
        await context.Publish(new AgvLockedChanged(agv.CorrelationId, false));
        await context.RespondAsync(new UnlockAgvResult(agv.CorrelationId));
        await context.Publish(new AgvModeChanged(agvCorrelationId, agv.Mode));
    }

    /// <summary>
    /// Handles the <see cref="AcceptAgvCommand"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<AcceptAgvCommand> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await TryGetAgv(correlationId);
        if (agv == null)
        {
            await context.RespondAsync(new AgvNotFoundResult(correlationId));
            return;
        }

        (bool accepted, bool stateChanged) = agv.TryAccept();
        if (!accepted)
        {
            _logger.LogDebug("Could not transition {AgvId} / {AgvCorrelationId} to idle in state {State}", agv.SerialNumber, agv.CorrelationId, Enum.GetName(agv.CurrentAgvState));
            await context.RespondAsync(new AcceptAgvRejected(agv.CorrelationId));
            return;
        }
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(AcceptAgvCommand), agv.CorrelationId);
        }
        _logger.LogDebug("AGV {AgvId} / {CorrelationId} was set to idle", agv.SerialNumber, agv.CorrelationId);
        await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        if (stateChanged)
        {
            await PublishStateChange(context, agv);
        }
        await context.RespondAsync(new AcceptAgvSucceeded(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="SetAgvForceIdle"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetAgvForceIdle> context)
    {
        var correlationId = context.Message.CorrelationId;

        var agv = await TryGetAgv(correlationId);
        if (agv == null)
        {
            await context.RespondAsync(new AgvNotFoundResult(correlationId));
            return;
        }
        bool stateChanged = agv.ForceIdle();
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(SetAgvForceIdle), agv.CorrelationId);
            return;
        }
        _logger.LogWarning("AGV {AgvId} / {AgvCorrelationId} was set to idle by force", agv.SerialNumber, agv.CorrelationId);
        await context.Publish(new AgvBecameIdle(correlationId));
        if (stateChanged)
        {
            await PublishStateChange(context, agv);
        }
        await context.RespondAsync(new SetAgvForceIdleResult(correlationId));
    }

    /// <summary>
    /// Handles the <see cref="HandleAgvActionStartFailure"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleAgvActionStartFailure> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        bool hasStartedAction = await _actionGroupRepository.HasStartedActionAsync(agv.CorrelationId);
        if (!hasStartedAction)
        {
            await TransitionToIdleAsync(context, agv, context.CancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="PrepareAgvForStartingAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PrepareAgvForStartingAction> context)
    {
        var (action, agvCorrelationId) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        bool isAvailable = agv.TryTransitionToBusy(false);

        if (!isAvailable)
        {
            _logger.LogWarning("Agv {AgvCorrelationId} is not available for busy", agv.CorrelationId);
            return;
        }
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId);
            return;
        }

        await PublishStateChange(context, agv);

        await context.Publish(new AgvPreparedForStartingAction(agvCorrelationId, action));
    }

    /// <summary>
    /// Handles the <see cref="UpdateCargo"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateCargo> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        IReadOnlyCollection<Cargo> receivedCargos = context.Message.Cargos;

        bool hadCargo = await _agvDb.Cargos.HasAgvCargoAsync(agvCorrelationId);
        var cargos = _mapper.Map<Collection<Domain.Models.Agv.Cargo>>(receivedCargos);
        if (await _agvDb.Cargos.SetCargoAsync(agvCorrelationId, cargos))
        {
            await context.Publish(new CargoChanged(agvCorrelationId, receivedCargos));
            var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
            _logger.LogAgvInformation(LogCodes.AgvCargoUpdated, agvCorrelationId, "Cargo Updated", $"AGV {agv.SerialNumber} cargo updated");
        }
        bool hasCargo = await _agvDb.Cargos.HasAgvCargoAsync(agvCorrelationId);
        if (hadCargo != hasCargo)
        {
            await context.Publish(new HasCargoChanged(agvCorrelationId, hasCargo));
        }
    }

    /// <summary>
    /// Handles the <see cref="ResetOfflineWatchdog"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetOfflineWatchdog> context)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(context.Message.CorrelationId);
        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        await ResetOfflineWatchDog(context, agv, agvSettings);
        await _agvDb.Agvs.UpdateAgvAsync(agv);
    }

    private async Task<Domain.Models.Agv.Agv?> TryGetAgv(Guid agvCorrelationId)
    {
        var agv = await _agvDb.Agvs.TryGetAgvAsync(agvCorrelationId);
        if (agv == null)
        {
            _logger.LogWarning("Agv {AgvCorrelationId} not found for responding to request.", agvCorrelationId);
        }
        return agv;
    }

    private async Task TryAutoAccept(IPublishEndpoint context, Domain.Models.Agv.Agv agv, CancellationToken cancellationToken)
    {
        _logger.LogDebug("AutoAccept {CorrelationId}", agv.CorrelationId);
        if (_productionOptions.AutoAccept && agv is { GraphExists: true, DataExist: true, CurrentAgvState: AgvState.Discovered })
        {
            _logger.LogDebug("AutoAccept {AgvCorrelationId}, Graph: {GraphExists}, Data: {DataExists}", agv.CorrelationId, agv.GraphExists, agv.DataExist);
            await TransitionToIdleAsync(context, agv, cancellationToken);
        }
    }

    private async Task TransitionToIdleAsync(IPublishEndpoint context, Domain.Models.Agv.Agv agv, CancellationToken cancellationToken)
    {
        bool stateChanged = agv.TransitionToIdle();
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(HandleActionCompletion), agv.CorrelationId);
            return;
        }

        if (!agv.AgvStatus!.Busy)
        {
            // publish also when state was already idle, to trigger further assignments
            await context.Publish(new AgvBecameIdle(agv.CorrelationId), cancellationToken);
        }

        if (stateChanged)
        {
            await PublishStateChange(context, agv);
            _logger.LogAgvInformation(LogCodes.AgvIdle, agv.CorrelationId, "AGV Became Idle", $"AGV {agv.SerialNumber} became idle");
        }
    }

    private async Task PublishStateChange(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvStateChanged(agv.CorrelationId, agv.CurrentAgvState, agv.PreviousAgvState, agvMessageContent));
    }

    private async Task PublishGraphRequestAsync(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        _logger.LogDebug("Requesting graph for agv {AgvCorrelationId}", agv.CorrelationId);
        await context.Publish(new RequestAgvGraph(agv.CorrelationId, agv.SerialNumber));
        agv.AgvGraphRequestedLastTime = DateTime.UtcNow;
    }

    private async Task PublishDataRequestAsync(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        _logger.LogDebug("Requesting data for agv {AgvCorrelationId}", agv.CorrelationId);
        await context.Publish(new RequestAgvData(agv.CorrelationId, agv.SerialNumber));
        agv.AgvDataRequestedLastTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an agv.
    /// </summary>
    private async Task CreateAgv(ConsumeContext<UpdateAgvStatus> context)
    {
        (var correlationId, string agvSerialNumber, var agvStatus) = context.Message;

        _logger.LogAgvInformation(LogCodes.AgvDiscovered, correlationId, "AGV Discovered", $"AGV {agvSerialNumber} has been discovered");
       
        var settingsGroup = await _agvDb.AgvSettings.GetSettingsGroupForAgv();
        var agv = new Domain.Models.Agv.Agv(correlationId, settingsGroup.Id,  agvSerialNumber, _mapper.Map<AgvStatus>(agvStatus));
        settingsGroup = await _agvDb.AgvSettings.GetSettingsGroupForAgv(agv);
        agv.AgvSettingsGroup = settingsGroup.Id;
        bool graphExists = await _agvDb.Graphs.ExistsAsync(correlationId);
        if (!graphExists)
        {
            await PublishGraphRequestAsync(context, agv);
        }

        agv.TransitionToDiscovered(graphExists);
        agv.AgvDataRequestedLastTime = DateTime.UtcNow;
        await _agvDb.Agvs.AddAgvAsync(agv);
        await PublishDataRequestAsync(context, agv);

        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvDiscovered(agvMessageContent));
    }

    private void LogBatteryStates(AgvStatus? oldAgvStatus, AgvStatus? newAgvStatus, AgvSettingsReadModel agvSettings)
    {
        if (newAgvStatus == null || oldAgvStatus == null)
        {
            return;
        }
        double oldSoc = oldAgvStatus.BatteryCharge;
        double newSoc = newAgvStatus.BatteryCharge;

        bool wasCharging = oldAgvStatus.Charging;
        bool isCharging = newAgvStatus.Charging;

        bool wasMinChargedUp = oldSoc >= agvSettings.MinChargeUp;
        bool isMinChargedUp = newSoc >= agvSettings.MinChargeUp;

        bool wasMaxChargedUp = oldSoc >= agvSettings.MaxChargeUp;
        bool isMaxChargedUp = newSoc >= agvSettings.MaxChargeUp;

        bool wasCritical = oldSoc >= agvSettings.BatteryLevelCritical;
        bool isCritical = newSoc >= agvSettings.BatteryLevelCritical;

        bool isLow = newSoc <= agvSettings.BatteryLevelLow;
        bool wasLow = oldSoc <= agvSettings.BatteryLevelLow;

        if (wasCharging && !isCharging)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryFullyCharged, oldAgvStatus.CorrelationId, "Battery Fully Charged", $"Battery has finished charging at {Math.Round(newAgvStatus.BatteryCharge)}%");
        }

        if (!wasMaxChargedUp && isMaxChargedUp)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryMaxChargeLevel, oldAgvStatus.CorrelationId, "Maximum Charging Level", $"Battery has reached maximum charging level {agvSettings.MaxChargeUp}%");
        }

        if (!wasMinChargedUp && isMinChargedUp)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryMinChargeLevel, oldAgvStatus.CorrelationId, "Minimum Charging Level", $"Battery has reached minimum charging level {agvSettings.MinChargeUp}%");
        }

        if (!wasCritical && isCritical)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryCriticalChargeLevel, oldAgvStatus.CorrelationId, "Critical Charging Level", $"Battery has reached critical charging level {agvSettings.BatteryLevelCritical}%");
        }

        if (!wasLow && isLow)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryLowChargeLevel, oldAgvStatus.CorrelationId, "Low Charging Level", $"Battery has reached low charging level {agvSettings.BatteryLevelLow}%");
        }

        if (isCharging && !wasCharging)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryStartedCharging, oldAgvStatus.CorrelationId, "Started Charging", $"Battery started charging at level {Math.Round(newAgvStatus.BatteryCharge)}%");
        }
    }

    private async Task PublishNodeStateChange(IPublishEndpoint context, string nodeId)
    {
        var nodeState = await _nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState == null)
        {
            _logger.LogError("Could not retrieve node state for node {NodeId}", nodeId);
            return;
        }
        var station = await _stationRepository.GetStation(nodeId);
        await context.Publish(new NodeStateChanged(nodeState, station.Id));
    }

    private async Task ResetLastNodeId(AgvStatus oldStatus, AgvStatus newStatus)
    {
        if (newStatus.LastNodeId == null || newStatus.LastNodeId == oldStatus.LastNodeId)
        {
            return;
        }

        // if the new last node id is not on a graph, do not update it
        if (await _agvDb.Graphs.TryGetNodeAsync(newStatus.LastNodeId) == null)
        {
            newStatus.UpdateLastNodeId(oldStatus.LastNodeId!);
        }
    }
}