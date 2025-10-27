using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Zones;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Utilities;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using Agv = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Agv;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;
using CreateQueueingActionGroup = Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup.CreateQueueingActionGroup;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvGraph;
using RequestAgvZones = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvZones;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides a handler for agv related commands.
/// </summary>
[UsedImplicitly]
public class AgvCommandHandler :
    IConsumer<TryReactivateChargingAgv>,
    IConsumer<AgvErrorChanged>,
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
    IConsumer<HandleActionAbortion>,
    IConsumer<PrepareAgvForStartingAction>,
    IConsumer<UpdateCargo>,
    IConsumer<PrepareIdleActionOfAgv>,
    IConsumer<PrepareChargingOfAgv>,
    IConsumer<UpdateAgvSettingsGroupReference>,
    IConsumer<TryReactivateParkingAgvs>,
    IConsumer<SwitchMode>,
    IConsumer<TryEnqueueAgv>,
    IConsumer<ReceiveAgvZones>,
    IConsumer<RemoveAgvFromAllZones>,
    IConsumer<RemoveAgvFromZone>,
    IConsumer<LeaveQueue>,
    IConsumer<SwitchChargingAgvs>,
    IConsumer<CheckAgvLoad>,
    IConsumer<RemoveAgvFromQueue>,
    IConsumer<RemoveAgvFromZones>
{
    private readonly ILogger<AgvCommandHandler> _logger;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _agvsSemaphore;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IMapper _mapper;
    private readonly SystemOptions _systemOptions;
    private readonly IAgvDb _agvDb;
    private readonly IParkPointService _parkPointService;
    private readonly IQueueManager _queueManager;
    private readonly IQueueRepository _queueRepository;
    private readonly INodeStateRepository _nodeStateRepository;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IStationRepository _stationRepository;
    private readonly ISemaphoreProvider<IQueueingSemaphore> _queueSemaphoreProvider;
    private readonly ILoggingService _loggingService;

    /// <inheritdoc cref="AgvCommandHandler"/>
    public AgvCommandHandler(ISemaphoreProvider<IAgvsSemaphore> agvsSemaphore,
        IAgvDb agvDb,
        IActionGroupRepository actionGroupRepository,
        IParkPointService parkPointService,
        IQueueManager queueManager,
        IChargingPointManager chargingPointManager,
        INodeStateRepository nodeStateRepository,
        IMapper mapper,
        SystemOptions systemOptions,
        IStationRepository stationRepository,
        ISemaphoreProvider<IQueueingSemaphore> queueSemaphoreProvider,
        IQueueRepository queueRepository,
        ILogger<AgvCommandHandler> logger,
        ILoggingService loggingService
    )
    {
        _logger = logger;
        _agvsSemaphore = agvsSemaphore;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _parkPointService = parkPointService;
        _queueManager = queueManager;
        _nodeStateRepository = nodeStateRepository;
        _mapper = mapper;
        _systemOptions = systemOptions;
        _chargingPointManager = chargingPointManager;
        _stationRepository = stationRepository;
        _queueSemaphoreProvider = queueSemaphoreProvider;
        _queueRepository = queueRepository;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Handles the <see cref="PrepareChargingOfAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PrepareChargingOfAgv> context)
    {
        var (agvCorrelationId, initiator) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (agv.IsLocked)
        {
            _logger.LogInformation("AGV:{CorrelationId} is locked. Do not prepare for charging", agv.CorrelationId);
            return;
        }

        if (agv.IsCharging)
        {
            _logger.LogDebug("No need for another charge action because the agv {SerialNumber}/{AgvCorrelationId} is already charging", agv.SerialNumber, agv.CorrelationId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        // Let's see if there is a created charge action group
        IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agvCorrelationId);
        var actionGroup = actionGroups.FirstOrDefault(a => a.Actions.Any(ac => ac.ActionType == ActionType.Charge));
        if (actionGroup != null)
        {
            _logger.LogWarning("No need for another charge action because the agv {SerialNumber}/{AgvCorrelationId} has already an assigned a charge action group", agv.SerialNumber, agv.CorrelationId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        // Are there any existing action groups in progress? We shouldn't attempt to charge unless the battery level is critical.
        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        bool agvMustCharge = agv.CheckMustCharge(agvSettings);
        bool hasNotStartedActionGroups = actionGroups.Any();
        if (!agvMustCharge && hasNotStartedActionGroups)
        {
            _logger.LogInformation("Did not prepare AGV:{CorrelationID} for charging, AGV has {Count} action groups that are not started and the following battery charge: {BatteryCharge}",
                agv.CorrelationId,
                actionGroups.Count,
                agv.AgvStatus!.BatteryCharge);
            return;
        }

        if (await _chargingPointManager.IsOnAChargingNodeAsync(agv))
        {
            _logger.LogInformation("Agv {AgvCorrelationId} is already on charger {NodeId}, no need to reserve one", agv.CorrelationId, agv.AgvStatus?.LastNodeId);
            await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, agv.AgvStatus!.LastNodeId!.Value, initiator));
            return;
        }

        var reservedChargingNode = await _chargingPointManager.GetReservedChargerForAgvAsync(agvCorrelationId);
        try
        {
            var nodeIdToPublish = reservedChargingNode?.NodeId ?? (await _chargingPointManager.ReserveChargerAsync(agv)).Id;
            await PublishNodeStateChange(context, nodeIdToPublish);
            await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, nodeIdToPublish, initiator));
        }
        catch (NoChargeStationAvailableException)
        {
            if (agvMustCharge)
            {
                await context.Publish(new CriticalAgvHasNoCharger(agv.CorrelationId));
            }
        }
    }

    /// <summary>
    /// Handles the <see cref="PrepareIdleActionOfAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PrepareIdleActionOfAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (agv.IsLocked)
        {
            return;
        }

        if (await _actionGroupRepository.HasCurrentAssignedActionGroupAsync(agvCorrelationId))
        {
            _logger.LogDebug("No need for an idle action because the agv {AgvCorrelationId} is already assigned to an action group", agvCorrelationId);
            await context.Publish(new AgvBecameIdle(agvCorrelationId));
            return;
        }

        // Idle hierarchy : queue, charge, park
        if (await TryQueueing(context, agv))
        {
            return;
        }

        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        if (agvSettings.InfiniteCharging)
        {
            if (await TryCharging(context, agv))
            {
                return;
            }
        }

        await TryParking(context, agv);
    }

    private async Task<bool> TryQueueing(IPublishEndpoint context, Agv agv)
    {
        if (agv.IsQueueing || await _queueManager.IsOnAQueueNode(agv.CorrelationId))
        {
            _logger.LogDebug("No need for a queue action because the agv is already in queuing for agvId: {AgvCorrelationId}", agv.CorrelationId);
            return true;
        }

        Guid? targetNodeId = await _queueManager.TryEnterQueue(agv);
        if (targetNodeId == null)
        {
            return false;
        }

        agv.IsQueueing = true;
        await _agvDb.Agvs.UpdateAgvAsync(agv);
        _logger.LogInformation("Starting queueing action for agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
        var targetNode = await _agvDb.Graphs.GetNodeAsync(targetNodeId.Value);
        await context.Publish(new QueueNodeSelectedForAgv(agv.CorrelationId, targetNode));
        return true;
    }

    private async Task<bool> TryCharging(IPublishEndpoint context, Agv agv)
    {
        if (await _chargingPointManager.IsOnAChargingNodeAsync(agv))
        {
            _logger.LogInformation("Agv {AgvCorrelationId} is already on charger {NodeId}, no need to reserve one", agv.CorrelationId, agv.AgvStatus?.LastNodeId);
            await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, agv.AgvStatus!.LastNodeId!.Value));
            return true;
        }

        var reservedChargingNode = await _chargingPointManager.GetReservedChargerForAgvAsync(agv.CorrelationId);
        Guid? chargingNodeId = reservedChargingNode?.NodeId;
        if (chargingNodeId == null)
        {
            try
            {
                var node = await _chargingPointManager.ReserveChargerAsync(agv);
                chargingNodeId = node.Id;
            }
            catch (NoChargeStationAvailableException)
            {
                _logger.LogInformation("No charger available for idling agv {AgvCorrelationId}, try parking", agv.CorrelationId);
                return false;
            }
        }

        await PublishNodeStateChange(context, chargingNodeId.Value);
        await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, chargingNodeId.Value));
        return true;
    }

    private async Task TryParking(IPublishEndpoint context, Agv agv)
    {
        if (await _parkPointService.IsOnAParkNodeAsync(agv) || await _parkPointService.AgvHasReservedParkingNodeAsync(agv.CorrelationId))
        {
            _logger.LogDebug("No need for a park action because the agv is already in parking for agvId: {AgvCorrelationId}", agv.CorrelationId);
            return;
        }

        bool hasParkers = await _parkPointService.HasParkersAsync(agv.CorrelationId);
        if (hasParkers)
        {
            _logger.LogInformation("Starting park action for agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
            var parkNode = await _parkPointService.ReserveParkAsync(agv);
            await PublishNodeStateChange(context, parkNode.Id);
            await context.Publish(new ParkingNodeSelectedForAgv(agv.CorrelationId, parkNode));
            return;
        }
        _logger.LogInformation("No parking stations defined");
    }

    /// <summary>
    /// Handles the <see cref="TryReactivateChargingAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryReactivateChargingAgv> context)
    {
        var cancellationToken = context.CancellationToken;
        await _agvsSemaphore.ExecuteLockedAsync(async () =>
        {
            ICollection<Agv> goodAgvs = await _agvDb.Agvs.GetChargingAgvsWithMinBatteryLevelAsync();

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
                    _logger.LogDebug("Agv is not charging at all with CorrelationId: {AgvCorrelationId}, so no need to abort it in order to get it to work again", agv.CorrelationId);
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
    /// Handles the <see cref="AgvErrorChanged"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvErrorChanged> context)
    {
        (var correlationId, List<AgvError> errors, _, _) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        bool hadError = agv.HasError;
        bool agvHasError = errors.HasAnyError();

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

        bool? wasDisturbed = oldStatus?.Disturbed;
        bool? wasAutomatic = oldStatus?.Automatic;

        bool zoneIdsChanged;
        if ((oldStatus == null || oldStatus.ZoneIds.IsNullOrEmpty()) && agvStatus.ZoneIds.IsNullOrEmpty())
        {
            zoneIdsChanged = false;
        }
        else
        {
            zoneIdsChanged = !(oldStatus?.ZoneIds?.SequenceEqual(agvStatus.ZoneIds!) ?? agvStatus.ZoneIds == null);
        }

        bool stationIdsChanged;
        if ((oldStatus == null || oldStatus.StationIds.IsNullOrEmpty()) && agvStatus.StationIds.IsNullOrEmpty())
        {
            stationIdsChanged = false;
        }
        else
        {
            stationIdsChanged = !(oldStatus?.StationIds?.SequenceEqual(agvStatus.StationIds!) ?? agvStatus.StationIds == null);
        }

        if (oldStatus != null && agv.GraphExists)
        {
            await ResetLastNodeId(oldStatus, agvStatus);
        }
        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        if (!wasOnline)
        {
            agv.SetOnline();
            await PrepareNodes(context, agvStatus, agv);
            await context.Publish(new CheckAgvLoad(agv.CorrelationId));
        }
        bool wasReady = agv.IsReady;
        (bool batteryStatusChanged, bool batteryChargeChanged, bool lastNodeIdChanged, bool operatingDataChanged) = agv.UpdateAgvStatus(agvStatus);
        bool isReady = agv.IsReady;

        await LogBatteryStates(oldStatus, agv.AgvStatus, agvSettings);

        if (!wasReady && isReady)
        {
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvReady, correlationId, null, "AGV Ready",
                $"AGV {agv.SerialNumber} became ready",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
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

        if (!agv.ZonesExist && agv.ShouldRequestZones())
        {
            await PublishZonesRequestAsync(context, agv);
        }

        if (agv.IsOnline != wasOnline)
        {
            await UpdateOfflineErrors(context, agv);
        }

        if (zoneIdsChanged)
        {
            await PublishAgvZonesChanged(context, agv.CorrelationId, agvStatus.ZoneIds);

            if (agv.IsOnline == wasOnline)
            {
                await UpdateOfflineErrors(context, agv, handleResourceErrors: false);
            }
        }

        if (stationIdsChanged)
        {
            await context.Publish(new AgvStationsChanged(agv.CorrelationId, agvStatus.StationIds!));
        }

        if (stationIdsChanged || zoneIdsChanged || batteryStatusChanged || batteryChargeChanged || lastNodeIdChanged || operatingDataChanged
            || isReady != wasReady || agv.IsOnline != wasOnline || wasDisturbed != agvStatus.Disturbed || wasAutomatic != agvStatus.Automatic)
        {
            if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
            {
                _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(UpdateAgvStatus), agv.CorrelationId);
                return;
            }

            if (agv.IsOnline != wasOnline || wasDisturbed != agvStatus.Disturbed || wasAutomatic != agvStatus.Automatic)
            {
                if (await _agvDb.Agvs.HasOnlineAgvs())
                {
                    await context.Publish(new AgvWentOnline(agv.CorrelationId));
                }
            }
        }
        _logger.LogTrace("{Command}: Agv {AgvCorrelationId}, operatingDataChanged: {OperatingDataChanged}, BatteryStatusChanged: {BatteryStatusChanged}, batteryChargeChanged: {BatteryChargeChanged}, lastNodeIdChanged: {LastNodeIdChanged}, wasOnline: {WWsOnline}",
            nameof(UpdateAgvStatus), agv.CorrelationId, operatingDataChanged, batteryStatusChanged, batteryChargeChanged, lastNodeIdChanged, wasOnline);

        await PublishAgvStatusUpdateEvents(context, agv, batteryStatusChanged, batteryChargeChanged, oldStatus, lastNodeIdChanged, wasOnline, operatingDataChanged);

        await TryAutoAccept(context, agv, context.CancellationToken);
    }

    private static async Task PublishAgvZonesChanged(IPublishEndpoint context, Guid agvId, ICollection<string>? zones)
    {
        await context.Publish(new AgvZonesChanged(agvId, zones ?? []));
    }

    private async Task PublishAgvStatusUpdateEvents(IPublishEndpoint context,
        Agv agv,
        bool batteryStatusChanged,
        bool batteryChargeChanged,
        AgvStatus? oldStatus,
        bool lastNodeIdChanged,
        bool wasOnline,
        bool operatingDataChanged)
    {
        var agvStatus = agv.AgvStatus!;

        if (batteryStatusChanged)
        {
            var agvBatteryStatus = new AgvBatteryStatus(_mapper.Map<Gebhardt.Acc.Common.Domain.Models.AgvStatus>(agvStatus));
            await context.Publish(new AgvBatteryStatusChanged(agvStatus.CorrelationId, agvBatteryStatus));
        }
        if (batteryChargeChanged)
        {
            await context.Publish(new AgvBatteryChargeChanged(agv.CorrelationId, oldStatus!.BatteryCharge, agvStatus.BatteryCharge));
        }

        if (lastNodeIdChanged)
        {
            await PublishLastNodeIdChange(context, agv, oldStatus!.LastNodeId, agvStatus.LastNodeId);
        }

        if (operatingDataChanged)
        {
            var agvOperatingDataStatus = new AgvOperatingDataStatus(_mapper.Map<Gebhardt.Acc.Common.Domain.Models.AgvStatus>(agvStatus));
            await context.Publish(new AgvOperatingDataStatusChanged(agvStatus.CorrelationId, agvOperatingDataStatus));
        }

        bool isIdle = agv.CurrentAgvState == AgvState.Idle && !agvStatus.Busy;
        bool idleSent = false;
        if (isIdle && oldStatus!.Busy)
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
            idleSent = true;
        }
        if (!wasOnline && agv.IsOnline)
        {
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, true));
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOnline, agvStatus.CorrelationId, null, "AGV Online",
                $"AGV {agv.SerialNumber} is online",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });

            if (isIdle && !idleSent)
            {
                await context.Publish(new AgvBecameIdle(agv.CorrelationId));
            }
        }
    }

    private async Task PublishLastNodeIdChange(IPublishEndpoint context, Agv agv, Guid? oldLastNodeId, Guid? newLastNodeId)
    {
        await context.Publish(new AgvLastNodeIdChanged(agv.CorrelationId, oldLastNodeId, newLastNodeId));
        Station? station;
        if (newLastNodeId != null)
        {
            station = await _stationRepository.GetRequiredStationBaseInformation(newLastNodeId.Value);
            if (station != null)
            {
                await _loggingService.LogStation(_logger, LogLevel.Information, LogCodes.StationAgvReached, station.Id, "AGV Reached Station",
                    $"AGV {agv.SerialNumber} reached station",
                    new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
            }
        }
        if (oldLastNodeId == null)
        {
            return;
        }

        station = await _stationRepository.GetRequiredStationBaseInformation(oldLastNodeId.Value);
        if (station != null)
        {
            await _loggingService.LogStation(_logger, LogLevel.Information, LogCodes.StationAgvLeft, station.Id, "AGV Left Station",
                $"AGV {agv.SerialNumber} left station",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        }
    }

    /// <summary>
    /// Handles the <see cref="HandleActionCompletion"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleActionCompletion> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been completed", correlationId);
        await TransitionToIdleAsync(context, correlationId, context.Message.IsQueueing, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="HandleActionAbortion"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleActionAbortion> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been aborted", correlationId);
        await TransitionToIdleAsync(context, correlationId, null, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="SetAgvGraphExist"/>
    /// </summary>
    public async Task Consume(ConsumeContext<SetAgvGraphExist> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);

        agv.GraphExists = true;

        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvGraphReceived, correlationId, null, "AGV Graph Received",
            $"AGV {agv.SerialNumber} graph has been received", new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        await _agvDb.Agvs.UpdateAgvAsync(agv);
        if (await _agvDb.Agvs.HasOnlineAgvs())
        {
            await context.Publish(new AgvWentOnline(Guid.NewGuid()));
        }
    }

    /// <summary>
    /// Handles the <see cref="GoOffline"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<GoOffline> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);

        bool wasOnline = agv.IsOnline;
        agv.GoOffline();
        if (wasOnline && !agv.IsOnline)
        {
            await UpdateOfflineErrors(context, agv, handleQueueBlockingErrors: true);
            if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
            {
                _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(GoOffline), agv.CorrelationId);
                return;
            }
            if (agv.AgvStatus?.ZoneIds?.Count > 0)
            {
                List<string> onlineAgvs = (await _agvDb.Agvs.GetAllAgvsAsync()).Where(a => a.IsOnline).Select(a => a.SerialNumber).ToList();
                await context.Publish(new AgvWentOfflineInZones(agv.CorrelationId, agv.SerialNumber, agv.AgvStatus!.ZoneIds, onlineAgvs));
            }
            await context.Publish(new ClearNodesFromAgv(agv.CorrelationId));
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, false));
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOffline, agv.CorrelationId, null, "AGV Offline",
                $"AGV {agv.SerialNumber} is offline",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        }

        if (!await _agvDb.Agvs.HasOnlineAgvs())
        {
            _logger.LogWarning("All agvs went offline");
            await context.Publish(new AllAgvsOffline());
        }
    }

    private async Task UpdateOfflineErrors(IPublishEndpoint context, Agv agv, bool handleZoneErrors = true, bool handleResourceErrors = true, bool handleQueueBlockingErrors = false)
    {
        IReadOnlyList<AgvError> activeErrors = await _agvDb.Errors.GetActiveErrorsAsync(agv.CorrelationId);
        List<AgvError> updatedErrors = activeErrors.ToList();
        var position = _agvDb.Positions.GetPosition(agv.CorrelationId);

        if (handleZoneErrors)
        {
            await UpdateZoneErrors(agv, updatedErrors);
        }

        if (handleResourceErrors)
        {
            await UpdateResourceBlockingErrors(agv, updatedErrors);
        }

        if (handleQueueBlockingErrors)
        {
            await UpdateQueueBlockingError(agv, updatedErrors);
        }

        await context.PublishSystemAgvErrorsAsync(agv.CorrelationId, activeErrors, updatedErrors, position.AgvPosition);
    }

    private async Task UpdateQueueBlockingError(Agv agv, List<AgvError> errors)
    {
        if (agv.IsOnline)
        {
            return;
        }
        if (agv.IsQueueing || await _queueRepository.GetQueueForAgv(agv.CorrelationId) != null)
        {
            bool hasQueueBlockingError = errors.Any(e => e.IsQueueBlockingError());
            if (!hasQueueBlockingError)
            {
                errors.Add(new QueueBlockingError(agv.CorrelationId, DateTime.UtcNow));
                _logger.LogInformation("Agv {AgvCorrelationId} went offline in queue or while heading towards queue", agv.CorrelationId);
            }
        }
    }

    private async Task UpdateResourceBlockingErrors(Agv agv, List<AgvError> errors)
    {
        bool hasResourceErrors = errors.Any(e => e.IsResourceError());

        // Remove ResourceBlockingError from agv errors
        if (agv.IsOnline && hasResourceErrors)
        {
            _logger.LogInformation("Removing resource blocking errors for agv {AgvCorrelationId}", agv.CorrelationId);
            errors.RemoveAll(e => e.IsResourceError());
            return;
        }

        // Add ResourceBlockingError(s)
        ICollection<NodeLock> nodeLocks = await _nodeStateRepository.GetNodeLocksForAgv(agv.CorrelationId);
        if (!hasResourceErrors && !agv.IsOnline && nodeLocks.Count > 0)
        {
            _logger.LogInformation("Adding resource blocking errors for agv {AgvCorrelationId}", agv.CorrelationId);
            foreach (var nodeLock in nodeLocks)
            {
                errors.Add(new ResourceBlockingError(agv.CorrelationId, DateTime.UtcNow, nodeLock.NodeId));
            }
        }
    }

    private async Task UpdateZoneErrors(Agv agv, List<AgvError> errors)
    {
        bool hasZoneErrors = errors.Any(e => e.IsZoneError());
        bool hasZonesInStatus = !agv.AgvStatus!.ZoneIds.IsNullOrEmpty();

        // Add ZoneError(s) to agv errors if it is not online
        if (hasZonesInStatus && !hasZoneErrors && !agv.IsOnline)
        {
            _logger.LogInformation("Adding zone errors for agv {AgvCorrelationId}", agv.CorrelationId);
            ICollection<Zone> zones = await _agvDb.Zones.GetZones(agv.AgvStatus!.ZoneIds!.ToArray());
            foreach (var zone in zones)
            {
                errors.Add(new ZoneError(agv.CorrelationId, DateTime.UtcNow, zone.ZoneId));
            }
            return;
        }

        // Remove ZoneError(s) from agv errors
        if ((agv.IsOnline || !hasZonesInStatus) && hasZoneErrors)
        {
            _logger.LogInformation("Removing zone errors for agv {AgvCorrelationId}", agv.CorrelationId);
            errors.RemoveAll(e => e.IsZoneError());
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
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvInformationReceived, correlationId, null, "AGV Information Received",
            $"AGV {agv.SerialNumber} information has been received", new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
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
        (var agvCorrelationId, string? initiatingUser, var assignedActionPolicy, var currentActionPolicy, bool cancelManualActions) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        agv.PrepareUserMode(initiatingUser, currentActionPolicy, assignedActionPolicy, cancelManualActions);

        await _agvDb.Agvs.UpdateAgvAsync(agv);
        await context.Publish(new AgvModeChangePending(agvCorrelationId, agv.Mode));
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvModeChangePending, agv.CorrelationId, null, "AGV Mode Change Pending",
            $"AGV {agv.SerialNumber} is pending mode change to {agv.Mode.PendingType.ToString()}",
            new Dictionary<string, object?>
            {
                { LogLabels.AgvSerialNumber, agv.SerialNumber },
                { LogLabels.AgvMode, agv.Mode.PendingType.ToString() },
                { LogLabels.User, agv.Mode.InitiatingUser },
                { LogLabels.Reason, agv.Mode.LockReason.ToString() ?? agv.Mode.MaintenanceReason.ToString() },
                { LogLabels.ReasonDescription, agv.Mode.ReasonDescription }
            });
    }

    /// <summary>
    /// Handles the <see cref="SetNormalMode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetNormalMode> context)
    {
        (var agvCorrelationId, string? initiatingUser) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        if (agv.Mode.Type == ModeType.Normal)
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
            return;
        }

        bool agvWasLocked = agv.IsLocked;
        bool wasMaintenanceMode = agv.IsInMaintenance;
        agv.SetNormalMode(initiatingUser);

        await _agvDb.Agvs.UpdateAgvAsync(agv);
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvModeChanged, agv.CorrelationId, null, "AGV Mode Changed",
            $"AGV {agv.SerialNumber} set to mode '{agv.Mode.Type.ToString()}'",
            new Dictionary<string, object?>
            {
                { LogLabels.AgvSerialNumber, agv.SerialNumber },
                { LogLabels.AgvMode, agv.Mode.Type.ToString() },
                { LogLabels.User, agv.Mode.InitiatingUser },
                { LogLabels.Reason, agv.Mode.LockReason.ToString() ?? agv.Mode.MaintenanceReason.ToString() },
                { LogLabels.ReasonDescription, agv.Mode.ReasonDescription }
            });

        if (agvWasLocked)
        {
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvUnlocked, agv.CorrelationId, null, "AGV Unlocked",
                $"AGV {agv.SerialNumber} is unlocked",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
            await context.Publish(new AgvLockedChanged(agv.CorrelationId, false));
        }

        if (wasMaintenanceMode)
        {
            await context.Publish(new CheckAgvLoad(agvCorrelationId));
        }

        await context.Publish(new AgvModeChanged(agvCorrelationId, agv.Mode));
        await context.Publish(new AgvBecameIdle(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="SetMaintenanceMode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetMaintenanceMode> context)
    {
        (var agvCorrelationId, string? initiatingUser, MaintenanceReason? maintenanceReason, string? reasonDescription, var assignedActionPolicy, var currentActionPolicy, bool cancelManualActions) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        agv.PrepareMaintenanceMode(maintenanceReason, reasonDescription, initiatingUser, currentActionPolicy, assignedActionPolicy, cancelManualActions);

        await _agvDb.Agvs.UpdateAgvAsync(agv);
        await context.Publish(new AgvModeChangePending(agvCorrelationId, agv.Mode));
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvModeChangePending, agv.CorrelationId, null, "AGV Mode Change Pending",
            $"AGV {agv.SerialNumber} is pending mode change to {agv.Mode.PendingType.ToString()}",
            new Dictionary<string, object?>
            {
                { LogLabels.AgvSerialNumber, agv.SerialNumber },
                { LogLabels.AgvMode, agv.Mode.PendingType.ToString() },
                { LogLabels.User, agv.Mode.InitiatingUser },
                { LogLabels.Reason, agv.Mode.MaintenanceReason.ToString() },
                { LogLabels.ReasonDescription, agv.Mode.ReasonDescription }
            });
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
        agv.PrepareLockedMode(context.Message.LockReason, context.Message.ReasonDescription, context.Message.InitiatingUser, context.Message.CurrentActionPolicy, context.Message.AssignedActionPolicy, context.Message.CancelManualActions);
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(LockAgv), agv.CorrelationId);
        }
        await context.RespondAsync(new LockAgvResult(agv.CorrelationId));
        await context.Publish(new AgvModeChangePending(agvCorrelationId, agv.Mode));
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvModeChangePending, agv.CorrelationId, null, "AGV Mode Change Pending",
            $"AGV {agv.SerialNumber} is pending mode change to locked mode",
            new Dictionary<string, object?>
            {
                { LogLabels.AgvSerialNumber, agv.SerialNumber },
                { LogLabels.AgvMode, agv.Mode.Type.ToString() },
                { LogLabels.User, agv.Mode.InitiatingUser },
                { LogLabels.Reason, agv.Mode.LockReason.ToString() },
                { LogLabels.ReasonDescription, agv.Mode.ReasonDescription }
            });
    }

    /// <summary>
    /// Handles the <see cref="SwitchMode"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SwitchMode> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await TryGetAgv(agvCorrelationId);
        if (agv == null)
        {
            _logger.LogError("Could not switch mode for agv {AgvCorrelationId}, agv not found", agvCorrelationId);
            return;
        }
        agv.SwitchMode();
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(SwitchMode), agv.CorrelationId);
        }
        if (agv.IsLocked)
        {
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvLocked, agv.CorrelationId, null, "AGV Locked", $"AGV {agv.SerialNumber} is locked",
                new Dictionary<string, object?>
                {
                    { LogLabels.AgvSerialNumber, agv.SerialNumber },
                    { LogLabels.AgvMode, agv.Mode.Type.ToString() },
                    { LogLabels.User, agv.Mode.InitiatingUser },
                    { LogLabels.Reason, agv.Mode.LockReason.ToString() ?? agv.Mode.MaintenanceReason.ToString() },
                    { LogLabels.ReasonDescription, agv.Mode.ReasonDescription }
                });
            await context.Publish(new AgvLockedChanged(agv.CorrelationId, true));
        }
        else
        {
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvModeChanged, agv.CorrelationId, null, "AGV Mode Changed", $"AGV {agv.SerialNumber} set to mode '{agv.Mode.Type.ToString()}'",
                new Dictionary<string, object?>
                {
                    { LogLabels.AgvSerialNumber, agv.SerialNumber },
                    { LogLabels.AgvMode, agv.Mode.Type.ToString() },
                    { LogLabels.User, agv.Mode.InitiatingUser },
                    { LogLabels.Reason, agv.Mode.LockReason.ToString() ?? agv.Mode.MaintenanceReason.ToString() },
                    { LogLabels.ReasonDescription, agv.Mode.ReasonDescription }
                });
        }
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
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvUnlocked, agv.CorrelationId, null, "AGV Unlocked",
            $"AGV {agv.SerialNumber} is unlocked",
            new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });

        // publish also when state was already idle, to trigger further assignments
        _logger.LogInformation("Agv {AgvCorrelationId} was unlocked", agvCorrelationId);
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
            await PublishStateChange(context, agv, context.CancellationToken);
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
            _logger.LogError("{Command}: Could not update agv {AgvCorrelationId}", nameof(SetAgvForceIdle), agv.CorrelationId);
            return;
        }
        _logger.LogWarning("AGV {AgvId} / {AgvCorrelationId} was set to idle by force", agv.SerialNumber, agv.CorrelationId);
        await context.Publish(new AgvBecameIdle(correlationId));
        if (stateChanged)
        {
            await PublishStateChange(context, agv, context.CancellationToken);
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

        if (!agv.TryTransitionToBusy(false, ignoreError: true))
        {
            _logger.LogInformation("Agv {AgvCorrelationId} is not available for busy", agv.CorrelationId);
            return;
        }

        var actionGroup = await _actionGroupRepository.GetActionGroupAsync(action.ParentId);
        if (actionGroup == null)
        {
            _logger.LogError("ActionGroup {ActionGroupCorrelationId} not found during preparation of agv {AgvCorrelationId} for starting", action.ParentId, agv.CorrelationId);
            return;
        }

        if (agv.HasError && !actionGroup.IsManual)
        {
            _logger.LogError("Agv {Agv} can not start automatic action {Action} of group {ActionGroup} while it has errors", agv.CorrelationId, action.CorrelationId, actionGroup.CorrelationId);
            return;
        }

        _logger.LogInformation("Agv with CorrelationId {AgvCorrelationId} was set to busy for action {Action} of group {ActionGroupCorrelationId}, IsQueueing: {IsQueueing}",
            agv.CorrelationId, action.CorrelationId, actionGroup.CorrelationId, agv.IsQueueing);
        if (actionGroup.Actions.Any(a => a.ActionType == ActionType.Charge) || actionGroup.IsManual)
        {
            await _queueSemaphoreProvider.ExecuteLockedAsync(async () =>
            {
                if (await TryEscapeQueue(context, agv, actionGroup.CorrelationId))
                {
                    agv.IsQueueing = false;
                }

                if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
                {
                    _logger.LogError("{Command}: Could not update agv {AgvCorrelationId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId);
                    return;
                }
                _logger.LogInformation("{Command}: Prepared agv {AgvCorrelationId} for starting action {ActionId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId, action.CorrelationId);
                await PublishStateChange(context, agv, context.CancellationToken);
                await context.Publish(new AgvPreparedForStartingAction(agvCorrelationId, action));
            });
            return;
        }
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogError("{Command}: Could not update agv {AgvCorrelationId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId);
            return;
        }
        await PublishStateChange(context, agv, context.CancellationToken);

        await _queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await TryDequeueAgv(context, agv, actionGroup.CorrelationId);
            _logger.LogInformation("{Command}: Prepared agv {AgvCorrelationId} for starting action {ActionId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId, action.CorrelationId);
            await context.Publish(new AgvPreparedForStartingAction(agvCorrelationId, action));
        });
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
        }
        bool hasCargo = await _agvDb.Cargos.HasAgvCargoAsync(agvCorrelationId);
        if (hadCargo != hasCargo)
        {
            await context.Publish(new HasCargoChanged(agvCorrelationId, hasCargo));
            var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
            await context.Publish(new CheckAgvLoad(agvCorrelationId));
            string cargoState = hasCargo ? "loaded" : "unloaded";
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvCargoUpdated, agvCorrelationId, null, "Cargo Updated",
                $"AGV {agv.SerialNumber} cargo has changed to {cargoState} ",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber }, { LogLabels.CargoState, cargoState } });
        }
    }

    private async Task TryDequeueAgv(IPublishEndpoint context, Agv agv, Guid currentActionGroup)
    {
        Dictionary<Guid, Guid>? agvsToMove = await _queueManager.TryDequeueAgv(agv.CorrelationId, agv.AgvStatus?.LastNodeId);
        if (agvsToMove == null)
        {
            return;
        }
        if (agvsToMove.Count == 0)
        {
            await context.Publish(new QueueBecameEmpty());
        }

        ICollection<Guid> queueEvacuationMoves = await _queueManager.GetQueueEvacuationMoves(agv.CorrelationId);
        await Task.WhenAll(queueEvacuationMoves.Where(a => a != currentActionGroup).Select(ag => context.Publish(new CancelOrAbortActionGroup(ag))));

        foreach (KeyValuePair<Guid, Guid> agvIdToMove in agvsToMove)
        {
            var agvToMove = await _agvDb.Agvs.GetAgvAsync(agvIdToMove.Key);
            if (agvToMove.IsQueueing)
            {
                var node = await _agvDb.Graphs.GetNodeAsync(agvIdToMove.Value);
                _logger.LogInformation("Trigger creating queue action for agv {AgvCorrelationId} to node {NodeId}", agvIdToMove.Key, agvIdToMove.Value);
                await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvIdToMove.Key, node));
            }
        }
        //Trigger refilling of queue
        await context.Publish(new TryReactivateParkingAgvs());
    }

    /// <summary>
    /// Handles the <see cref="TryEnqueueAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryEnqueueAgv> context)
    {
        var (agvCorrelationId, entryNode) = context.Message;

        await _queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            Guid? targetNodeId = await _queueManager.TryEnqueueAgv(agvCorrelationId, entryNode);
            if (targetNodeId != null && targetNodeId != entryNode)
            {
                var targetNode = await _agvDb.Graphs.GetNodeAsync(targetNodeId.Value);
                await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvCorrelationId, targetNode));
                await context.Publish(new QueueEntered());
            }

            if (targetNodeId == entryNode)
            {
                await context.Publish(new QueueEntered());
            }
        });
    }

    private async Task<bool> TryEscapeQueue(IPublishEndpoint context, Agv agv, Guid currentActionGroup)
    {
        var agvCorrelationId = agv.CorrelationId;
        Guid? agvLastNodeId = agv.AgvStatus?.LastNodeId;

        (Dictionary<Guid, Guid>? agvsToMove, int? queueSize) = await _queueManager.TryEscapeQueue(agvCorrelationId, agvLastNodeId);
        if (agvsToMove == null)
        {
            return false;
        }

        List<Guid> actionGroupsToAbort = (await _queueManager.GetQueueInternalActionGroups(agv)).ToList();
        ICollection<Guid> queueEvacuationMoves = await _queueManager.GetQueueEvacuationMoves(agv.CorrelationId);
        actionGroupsToAbort.AddRange(queueEvacuationMoves);
        await Task.WhenAll(actionGroupsToAbort.Where(a => a != currentActionGroup).Select(ag => context.Publish(new CancelOrAbortActionGroup(ag))));
        if (queueSize == 0)
        {
            await context.Publish(new QueueBecameEmpty());
        }
        foreach (var (agvToMoveId, nodeId) in agvsToMove)
        {
            var agvToMove = await _agvDb.Agvs.GetAgvAsync(agvToMoveId);
            if (!agvToMove.IsQueueing)
            {
                continue;
            }
            _logger.LogInformation("Trigger creating queue action for agv {AgvCorrelationId} to node {NodeId}", agvToMoveId, nodeId);
            var node = await _agvDb.Graphs.GetNodeAsync(nodeId);
            await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvToMoveId, node));
        }
        //Trigger refilling of queue
        await context.Publish(new TryReactivateParkingAgvs());
        return true;
    }

    /// <summary>
    /// Handles the <see cref="TryReactivateParkingAgvs"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryReactivateParkingAgvs> context)
    {
        ICollection<Guid> agvsAtQueueHeads = await _queueManager.GetAgvsAtQueueHeads();
        ICollection<Agv> readyAgvs = await _agvDb.Agvs.GetReadyAgvsWithAtLeastLowBatteryLevel();

        foreach (var agv in agvsAtQueueHeads.Union(readyAgvs.Select(a => a.CorrelationId)))
        {
            await context.Publish(new AgvBecameIdle(agv));
        }
    }

    /// <summary>
    /// Handles the <see cref="ReceiveAgvZones"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ReceiveAgvZones> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        ICollection<AgvZone> zones = context.Message.AgvZones;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        agv.ZonesExist = true;
        await _agvDb.Agvs.UpdateAgvAsync(agv);

        await Task.WhenAll(zones.Select(z => context.Publish(new AddZone(z))));
    }

    /// <summary>
    /// Handles the <see cref="RemoveAgvFromAllZones"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromAllZones> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (agv.IsOnline)
        {
            return;
        }

        agv.AgvStatus!.ResetZones();
        await UpdateOfflineErrors(context, agv, handleResourceErrors: false);
        await _agvDb.Agvs.UpdateAgvAsync(agv);
    }

    /// <summary>
    /// Handles the <see cref="RemoveAgvFromZone"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromZone> context)
    {
        (var agvCorrelationId, string zoneId, _) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (agv.IsOnline)
        {
            return;
        }

        bool wasRemoved = agv.AgvStatus!.RemoveZone(zoneId);

        if (!wasRemoved)
        {
            return;
        }

        await UpdateOfflineErrors(context, agv, handleResourceErrors: false);
        await _agvDb.Agvs.UpdateAgvAsync(agv);
        await context.Publish(new AgvZonesChanged(agv.CorrelationId, agv.AgvStatus?.ZoneIds ?? []));
    }

    private async Task<Agv?> TryGetAgv(Guid agvCorrelationId)
    {
        var agv = await _agvDb.Agvs.TryGetAgvAsync(agvCorrelationId);
        if (agv == null)
        {
            _logger.LogWarning("Agv {AgvCorrelationId} not found for responding to request", agvCorrelationId);
        }
        return agv;
    }

    private async Task TryAutoAccept(IPublishEndpoint context, Agv agv, CancellationToken cancellationToken)
    {
        _logger.LogDebug("AutoAccept {CorrelationId}", agv.CorrelationId);
        if (_systemOptions.AutoAccept && agv is { GraphExists: true, DataExist: true, CurrentAgvState: AgvState.Discovered })
        {
            _logger.LogDebug("AutoAccept {AgvCorrelationId}, Graph: {GraphExists}, Data: {DataExists}", agv.CorrelationId, agv.GraphExists, agv.DataExist);
            await TransitionToIdleAsync(context, agv, cancellationToken);
        }
    }

    private async Task TransitionToIdleAsync(IPublishEndpoint publishEndpoint, Guid agvCorrelationId, bool? isQueueing, CancellationToken cancellationToken)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        if (isQueueing.HasValue)
        {
            agv.IsQueueing = isQueueing.Value;
        }
        await TransitionToIdleAsync(publishEndpoint, agv, cancellationToken);
    }

    private async Task TransitionToIdleAsync(IPublishEndpoint publishEndpoint, Agv agv, CancellationToken cancellationToken)
    {
        bool stateChanged = agv.TransitionToIdle();
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("Could not update agv {AgvCorrelationId}", agv.CorrelationId);
            return;
        }

        if (!agv.AgvStatus!.Busy)
        {
            // publish also when state was already idle, to trigger further assignments
            await publishEndpoint.Publish(new AgvBecameIdle(agv.CorrelationId), cancellationToken);
        }

        if (stateChanged)
        {
            await PublishStateChange(publishEndpoint, agv, cancellationToken);
            await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvIdle, agv.CorrelationId, null, "AGV Became Idle",
                $"AGV {agv.SerialNumber} became idle",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        }
    }

    private async Task PublishStateChange(IPublishEndpoint context, Agv agv, CancellationToken cancellationToken)
    {
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvStateChanged(agv.CorrelationId, agv.CurrentAgvState, agv.PreviousAgvState, agvMessageContent), cancellationToken);
    }

    private async Task PublishGraphRequestAsync(IPublishEndpoint context, Agv agv)
    {
        _logger.LogDebug("Requesting graph for agv {AgvCorrelationId}", agv.CorrelationId);
        await context.Publish(new RequestAgvGraph(agv.CorrelationId, agv.SerialNumber));
        agv.AgvGraphRequestedLastTime = DateTime.UtcNow;
    }

    private async Task PublishDataRequestAsync(IPublishEndpoint context, Agv agv)
    {
        _logger.LogDebug("Requesting data for agv {AgvCorrelationId}", agv.CorrelationId);
        await context.Publish(new RequestAgvData(agv.CorrelationId, agv.SerialNumber));
        agv.AgvDataRequestedLastTime = DateTime.UtcNow;
    }

    private async Task PublishZonesRequestAsync(IPublishEndpoint context, Agv agv)
    {
        _logger.LogDebug("Requesting zones for agv {AgvCorrelationId}", agv.CorrelationId);
        await context.Publish(new RequestAgvZones(agv.CorrelationId, agv.SerialNumber));
        agv.AgvZonesRequestedLastTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an agv.
    /// </summary>
    private async Task CreateAgv(ConsumeContext<UpdateAgvStatus> context)
    {
        (var correlationId, string agvSerialNumber, var agvStatus) = context.Message;

        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvDiscovered, correlationId, null, "AGV Discovered", $"AGV {agvSerialNumber} has been discovered",
            new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agvSerialNumber } });

        var settingsGroup = await _agvDb.AgvSettings.GetSettingsGroupForAgv();
        var agv = new Agv(correlationId, settingsGroup.Id, agvSerialNumber, _mapper.Map<AgvStatus>(agvStatus));
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

    private async Task LogBatteryStates(AgvStatus? oldAgvStatus, AgvStatus? newAgvStatus, AgvSettingsReadModel agvSettings)
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

        bool wasCritical = oldSoc <= agvSettings.BatteryLevelCritical;
        bool isCritical = newSoc <= agvSettings.BatteryLevelCritical;

        bool isLow = newSoc <= agvSettings.BatteryLevelLow;
        bool wasLow = oldSoc <= agvSettings.BatteryLevelLow;

        if (wasCharging && !isCharging)
        {
            await _loggingService.LogBattery(_logger, LogLevel.Information, LogCodes.BatteryFullyCharged, oldAgvStatus.CorrelationId, null,
                "Battery Fully Charged", $"Battery has finished charging at {Math.Round(newAgvStatus.BatteryCharge)}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, Math.Round(newAgvStatus.BatteryCharge) } });
        }

        if (!wasMaxChargedUp && isMaxChargedUp && !agvSettings.InfiniteCharging)
        {
            await _loggingService.LogBattery(_logger, LogLevel.Information, LogCodes.BatteryMaxChargeLevel, oldAgvStatus.CorrelationId, null,
                "Maximum Charging Level", $"Battery has reached maximum charging level {agvSettings.MaxChargeUp}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, agvSettings.MaxChargeUp } });
        }

        if (!wasMinChargedUp && isMinChargedUp)
        {
            await _loggingService.LogBattery(_logger, LogLevel.Information, LogCodes.BatteryMinChargeLevel, oldAgvStatus.CorrelationId, null,
                "Minimum Charging Level", $"Battery has reached minimum charging level {agvSettings.MinChargeUp}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, agvSettings.MinChargeUp } });
        }

        if (!wasCritical && isCritical)
        {
            await _loggingService.LogBattery(_logger, LogLevel.Information, LogCodes.BatteryCriticalChargeLevel, oldAgvStatus.CorrelationId, null,
                "Critical Charging Level",
                $"Battery has reached critical charging level {agvSettings.BatteryLevelCritical}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, agvSettings.BatteryLevelCritical } });
        }

        if (!wasLow && isLow)
        {
            await _loggingService.LogBattery(_logger, LogLevel.Information, LogCodes.BatteryLowChargeLevel, oldAgvStatus.CorrelationId, null,
                "Low Charging Level", $"Battery has reached low charging level {agvSettings.BatteryLevelLow}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, agvSettings.BatteryLevelLow } });
        }
        if (isCharging && !wasCharging)
        {
            await _loggingService.LogBattery(_logger, LogLevel.Information, LogCodes.BatteryStartedCharging, oldAgvStatus.CorrelationId, null,
                "Started Charging", $"Battery started charging at level {Math.Round(newAgvStatus.BatteryCharge)}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, Math.Round(newAgvStatus.BatteryCharge) } });
        }
    }

    private async Task PublishNodeStateChange(IPublishEndpoint context, Guid nodeId)
    {
        var nodeState = await _nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState == null)
        {
            _logger.LogError("Could not retrieve node state for node {NodeId}", nodeId);
            return;
        }
        try
        {
            var station = await _stationRepository.GetStation(nodeId);
            await context.Publish(new NodeStateChanged(nodeState, station.Id, station.LockedBy, station.LockReason, station.LockDescription));
        }
        catch (NodeNotFoundException)
        {
            await context.Publish(new NodeStateChanged(nodeState, null, null, null, null));
        }
    }

    private async Task ResetLastNodeId(AgvStatus oldStatus, AgvStatus newStatus)
    {
        if (newStatus.LastNodeId == null || newStatus.LastNodeId == oldStatus.LastNodeId)
        {
            return;
        }

        // if the new last node id is not on a graph, do not update it
        if (await _agvDb.Graphs.TryGetNodeByIdAsync(newStatus.LastNodeId!.Value) == null)
        {
            newStatus.UpdateLastNodeId(oldStatus.LastNodeId);
        }
    }

    private async Task PrepareNodes(IPublishEndpoint context, AgvStatus agvStatus, Agv agv)
    {
        _logger.LogInformation("Preparing nodes for AGV {AgvCorrelationId}. Last node id of AGV is {LastNodeId}", agv.CorrelationId, agvStatus.LastNodeId);

        IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agv.CorrelationId);
        if (actionGroups.Count < 1 && agvStatus.LastNodeId == Guid.Empty)
        {
            return;
        }
        List<Guid> lockedNodes = (await _nodeStateRepository.GetNodeLocksForAgv(agv.CorrelationId)).Select(n => n.NodeId).ToList();

        List<Guid> currentNodeIds = actionGroups.SelectMany(a => a.Actions).Select(a => a.Target.Node?.Id).Where(t => t != null).Select(t => t!.Value).ToList();
        if (agvStatus.LastNodeId != null && agvStatus.LastNodeId != Guid.Empty)
        {
            currentNodeIds.Add(agvStatus.LastNodeId.Value);
        }
        ICollection<GraphNode> currentNodes = (await _agvDb.Graphs.GetNodesAsync(n => currentNodeIds.Contains(n.Id))).ToList();
        IEnumerable<Guid> parkers = currentNodes.Where(t => t.IsParker()).Select(t => t.Id);

        var now = DateTime.UtcNow;
        foreach (var node in currentNodes.Where(n => n.NeedsReservation() && !lockedNodes.Contains(n.Id)))
        {
            var actionGroup = actionGroups.FirstOrDefault(a => a.Actions.Any(ac => ac.Target.Node?.Id == node.Id));
            if (actionGroup != null && actionGroup.IsOutdated(Agv.OfflineTolerance, now))
            {
                _logger.LogWarning("Action group {ActionGroupCorrelationId} outdated after agv {AgvCorrelationId} went back online", actionGroup.CorrelationId, agv.CorrelationId);
                await context.Publish(new CancelOrAbortActionGroup(actionGroup.CorrelationId));
                continue;
            }

            if (node.Id == agvStatus.LastNodeId)
            {
                _logger.LogInformation("Preparing nodes for AGV {AgvCorrelationId}. Occupy node {NodeId}", agv.CorrelationId, node.Name);

                await _nodeStateRepository.ForceOccupyNodeAsync(node.Id, agv.CorrelationId);
                await PublishNodeStateChange(context, node.Id);
                continue;
            }

            if (!await _nodeStateRepository.ReserveNodeAsync(node.Id, agv.CorrelationId))
            {
                if (actionGroup != null)
                {
                    _logger.LogWarning("Could not restore reservation of node {NodeId} for Agv {AgvCorrelationId}, aborting action group {ActionGroupCorrelationId}", node.Name, agv.CorrelationId, actionGroup.CorrelationId);
                    await context.Publish(new CancelOrAbortActionGroup(actionGroup.CorrelationId));
                    continue;
                }
                await PublishNodeStateChange(context, node.Id);
            }
        }

        foreach (var moveToQueue in actionGroups.Where(a => parkers.All(p => p != a.Actions.First().Target.Node?.Id) && a is { IsMovingCargo: false, IsChargingOrder: false, IsManual: false }))
        {
            if (!moveToQueue.IsOutdated(Agv.OfflineTolerance, now))
            {
                continue;
            }
            _logger.LogWarning("Queuing action group {ActionGroupCorrelationId} outdated after agv {AgvCorrelationId} went back online", moveToQueue.CorrelationId, agv.CorrelationId);
            await context.Publish(new CancelOrAbortActionGroup(moveToQueue.CorrelationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="LeaveQueue"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<LeaveQueue> context)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(context.Message.CorrelationId);
        agv.IsQueueing = false;
        await _agvDb.Agvs.UpdateAgvAsync(agv);
    }

    /// <summary>
    /// Handles the <see cref="SwitchChargingAgvs"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SwitchChargingAgvs> context)
    {
        var (nonChargingAgvId, chargingAgvId) = context.Message;

        var nonChargingAgv = await _agvDb.Agvs.GetAgvAsync(nonChargingAgvId);
        var chargingAgv = await _agvDb.Agvs.GetAgvAsync(chargingAgvId);

        _logger.LogInformation("Try switching charging agv {AgvCorrelationId} with non-charging agv {NonChargingAgvCorrelationId}", chargingAgv.CorrelationId, nonChargingAgv.CorrelationId);

        ICollection<NodeLock> nodeLocks = await _nodeStateRepository.GetNodeLocksForAgv(chargingAgvId);
        Guid? chargingNode = nodeLocks.FirstOrDefault(n => n.IsOccupiedByAgv(chargingAgvId))?.NodeId ?? chargingAgv.AgvStatus?.LastNodeId;

        if (chargingNode == null)
        {
            _logger.LogError("{Command}: Could not find charger for switching {ChargingAgv} with {NonChargingAgv}", nameof(SwitchChargingAgvs), chargingAgv.CorrelationId, nonChargingAgv.CorrelationId);
            await context.Publish(new SwitchChargingAgvsFailed(nonChargingAgvId, chargingAgvId));
            return;
        }

        bool foundQueueToEnter = false;

        await _queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            foundQueueToEnter = await TrySwapAgvsInQueue(context, nonChargingAgv, chargingAgv);
            if (foundQueueToEnter)
            {
                chargingAgv.IsQueueing = true;
                await _agvDb.Agvs.UpdateAgvAsync(chargingAgv);
            }
        });

        if (foundQueueToEnter)
        {
            await TakeOverCharger(context, chargingNode.Value, nonChargingAgv, chargingAgv, nonChargingAgvId);
            return;
        }

        if (!await TryParking(context, chargingAgv, nonChargingAgv))
        {
            Guid? fallbackSwitchPositionId = nonChargingAgv.AgvStatus?.LastNodeId;
            if (fallbackSwitchPositionId == null)
            {
                _logger.LogError("{Command}: Cannot use fallback of LastNodeId to switch out {ChargingAgvCorrelationId} with {NonChargingAgvCorrelationId}", nameof(SwitchChargingAgvs), chargingAgv.CorrelationId, nonChargingAgv.CorrelationId);
                await context.Publish(new SwitchChargingAgvsFailed(nonChargingAgvId, chargingAgvId));
                return;
            }

            var fallbackSwitchPosition = await _agvDb.Graphs.GetNodeAsync(fallbackSwitchPositionId.Value);
            _logger.LogInformation("Using fallback of LastNodeId {LastNodeId} to switch out {ChargingAgvCorrelationId} with {NonChargingAgvCorrelationId}", fallbackSwitchPosition, chargingAgv.CorrelationId, nonChargingAgv.CorrelationId);

            var groupCorrelationId = Guid.NewGuid();
            var actionGroup = new ActionGroup(
                groupCorrelationId,
                $"Move agv {chargingAgv.SerialNumber}",
                actions: new List<Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, fallbackSwitchPosition, groupCorrelationId) },
                assignedAgv: chargingAgv.CorrelationId,
                creator: ActionGroupCreator.TransportOrder,
                hasPriority: true,
                isQueued: true
            );
            await context.Publish(new QueueActionGroup(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), false));
        }

        await TakeOverCharger(context, chargingNode.Value, nonChargingAgv, chargingAgv, nonChargingAgvId);
    }

    private async Task TakeOverCharger(IPublishEndpoint context, Guid chargingNodeId, Agv nonChargingAgv, Agv chargingAgv, Guid nonChargingAgvId)
    {
        if (!await _nodeStateRepository.ForceReserveNodeAsync(chargingNodeId, nonChargingAgv.CorrelationId))
        {
            _logger.LogError("{Command}: Cannot reserve charger {Charger} to switch out {ChargingAgvCorrelationId} with {NonChargingAgvCorrelationId}", nameof(SwitchChargingAgvs), chargingNodeId, chargingAgv.CorrelationId, nonChargingAgv.CorrelationId);
            return;
        }

        await context.Publish(new ChargingNodeSelectedForAgv(nonChargingAgvId, chargingNodeId));
        await PublishNodeStateChange(context, chargingNodeId);
        await context.Publish(new AbortChargeAction(chargingAgv.CorrelationId));
    }

    private async Task<bool> TrySwapAgvsInQueue(IPublishEndpoint context, Agv queuingAgv, Agv agvToQueue)
    {
        (Dictionary<Guid, Guid>? agvsToMove, Guid? queueEntryNodeId, bool swapped) = await _queueManager.TrySwapAgv(queuingAgv, agvToQueue);
        if (!swapped || queueEntryNodeId == null)
        {
            return false;
        }

        var queueEntry = await _agvDb.Graphs.GetNodeAsync(queueEntryNodeId.Value);
        if (agvsToMove == null)
        {
            await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvToQueue.CorrelationId, queueEntry));
            return true;
        }

        ICollection<Guid> actionGroupsToAbort = await _queueManager.GetQueueInternalActionGroups(queuingAgv);
        await Task.WhenAll(actionGroupsToAbort.Select(ag => context.Publish(new CancelOrAbortActionGroup(ag))));

        foreach (var (agvToMoveId, nodeId) in agvsToMove)
        {
            var agvToMove = await _agvDb.Agvs.GetAgvAsync(agvToMoveId);
            if (!agvToMove.IsQueueing)
            {
                continue;
            }
            _logger.LogInformation("Trigger creating queue action for agv {AgvCorrelationId} to node {NodeId}", agvToMoveId, nodeId);
            var node = await _agvDb.Graphs.GetNodeAsync(nodeId);
            await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvToMoveId, node));
        }
        await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvToQueue.CorrelationId, queueEntry));
        return true;
    }

    private async Task<bool> TryParking(IPublishEndpoint context, Agv chargingAgv, Agv nonChargingAgv)
    {
        bool hasParkers = await _parkPointService.HasParkersAsync(chargingAgv.CorrelationId);
        if (!hasParkers)
        {
            _logger.LogInformation("No parking stations defined");
            return false;
        }

        if (await _parkPointService.IsOnAParkNodeAsync(nonChargingAgv) || await _parkPointService.AgvHasReservedParkingNodeAsync(nonChargingAgv.CorrelationId))
        {
            Guid? parker = (await _nodeStateRepository.GetNodeLocksForAgv(nonChargingAgv.CorrelationId)).FirstOrDefault()?.NodeId;
            if (parker != null)
            {
                await _nodeStateRepository.ForceReserveNodeAsync(parker.Value, chargingAgv.CorrelationId);
                _logger.LogInformation("Starting park action to {ParkNode} for agv with CorrelationId: {AgvCorrelationId}", parker, chargingAgv.CorrelationId);
                await PublishNodeStateChange(context, parker.Value);
                var parkerNode = await _agvDb.Graphs.GetNodeAsync(parker.Value);
                await context.Publish(new ParkingNodeSelectedForAgv(chargingAgv.CorrelationId, parkerNode));
                return true;
            }
        }

        try
        {
            var parkNode = await _parkPointService.ReserveParkAsync(chargingAgv);
            _logger.LogInformation("Starting park action to {ParkNode} for agv with CorrelationId: {AgvCorrelationId}", parkNode, chargingAgv.CorrelationId);
            await PublishNodeStateChange(context, parkNode.Id);
            await context.Publish(new ParkingNodeSelectedForAgv(chargingAgv.CorrelationId, parkNode));
            return true;
        }
        catch (ParkNodeNotAvailableException)
        {
            _logger.LogWarning("No parking stations found for agv {AgvCorrelationId}", nonChargingAgv.CorrelationId);
            return false;
        }
    }

    /// <summary>
    /// Handles the <see cref="CheckAgvLoad"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CheckAgvLoad> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        bool hasCargo = await _agvDb.Cargos.HasAgvCargoAsync(agvCorrelationId);

        IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRepository.GetActionGroupsAsync(states: [ActionGroupState.Completed, ActionGroupState.Paused, ActionGroupState.Started], assignedAgvCorrelationId: agvCorrelationId, maxItems: 3, sortBy: SortBy.StartedDate);

        var actionGroup = actionGroups.SingleOrDefault(a => a.State == ActionGroupState.Started) ?? actionGroups.OrderBy(a => a.StartedDate).LastOrDefault();

        if (actionGroup == null)
        {
            _logger.LogError("No recent action groups for agv {AgvCorrelationId}, could not check load state", agvCorrelationId);
            return;
        }

        IReadOnlyList<AgvError> activeErrors = (await _agvDb.Errors.GetActiveErrorsAsync(agvCorrelationId)).ToList();
        List<AgvError> updatedErrors = activeErrors.ToList();
        bool hasCargoError = activeErrors.Any(e => e.IsCargoMismatchError());
        var position = _agvDb.Positions.GetPosition(agvCorrelationId);

        if (actionGroup.CheckLoadStateMatches(hasCargo, agvCorrelationId))
        {
            _logger.LogDebug("Load state {HasCargo} of agv {AgvCorrelationId} matches state of order {ActionGroupCorrelationId}", hasCargo, agvCorrelationId, actionGroup.CorrelationId);
            if (hasCargoError)
            {
                _logger.LogInformation("Removing cargo mismatch error for agv {AgvCorrelationId}, cargo state {HasCargo} matches order state", agvCorrelationId, hasCargo);

                updatedErrors.RemoveAll(e => e.IsCargoMismatchError());
                await context.PublishSystemAgvErrorsAsync(agvCorrelationId, activeErrors, updatedErrors, position.AgvPosition);
            }
            return;
        }
        if (hasCargoError)
        {
            _logger.LogWarning("Agv {AgvCorrelationId} keeps having cargo mismatch error, cargo state {HasCargo} does not match state of order {ActionGroupCorrelationId}", agvCorrelationId, hasCargo, actionGroup.CorrelationId);
            return;
        }
        _logger.LogWarning("Agv {AgvCorrelationId} has cargo mismatch error, cargo state {HasCargo} does not match state of order {ActionGroupCorrelationId}", agvCorrelationId, hasCargo, actionGroup.CorrelationId);
        updatedErrors.Add(new CargoMismatchError(agvCorrelationId, DateTime.UtcNow, hasCargo));
        await context.PublishSystemAgvErrorsAsync(agvCorrelationId, activeErrors, updatedErrors, position.AgvPosition);
    }

    /// <summary>
    /// Handles the <see cref="RemoveAgvFromQueue"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromQueue> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        bool removed = false;
        await _queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (context.Message.AlsoRemoveFromConsideration)
            {
                List<Queue> queues = (await _queueRepository.GetQueues()).Where(q => q.ConsideredAgvs.Contains(agv.CorrelationId)).ToList();

                foreach (var queue in queues)
                {
                    queue.ConsideredAgvs.Remove(agv.CorrelationId);
                }

                await Task.WhenAll(queues.Select(_queueRepository.UpdateQueueAsync));
            }

            removed = await TryEscapeQueue(context, agv, Guid.Empty);
        });

        if (!removed)
        {
            removed = await _queueManager.RemoveAgvFromQueueEntry(agvCorrelationId, null);
        }

        if (!removed)
        {
            return;
        }

        agv.IsQueueing = false;

        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogInformation("{Command}: Could not update agv {AgvCorrelationId}", nameof(RemoveAgvFromQueue), agv.CorrelationId);
            return;
        }

        IReadOnlyList<AgvError> activeErrors = await _agvDb.Errors.GetActiveErrorsAsync(agvCorrelationId);
        List<AgvError> updatedErrors = activeErrors.ToList();
        var blockingError = updatedErrors.SingleOrDefault(e => e.IsQueueBlockingError());

        if (blockingError == null)
        {
            return;
        }

        updatedErrors.Remove(blockingError);
        var position = _agvDb.Positions.GetPosition(agvCorrelationId);
        await context.PublishSystemAgvErrorsAsync(agv.CorrelationId, activeErrors, updatedErrors, position.AgvPosition);
    }

    /// <summary>
    /// Handles the agv <see cref="RemoveAgvFromZones"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromZones> context)
    {
        (var agvCorrelationId, ICollection<string> removedZones) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        if (agv.AgvStatus?.ZoneIds?.Count > 0)
        {
            _logger.LogInformation("Remove zones {Zones} from status of agv {AgvCorrelationId}", string.Join(",", removedZones), agvCorrelationId);
            agv.AgvStatus.RemoveZones(removedZones);

            if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
            {
                _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(UpdateAgvStatus), agv.CorrelationId);
                return;
            }

            await PublishAgvZonesChanged(context, agv.CorrelationId, agv.AgvStatus.ZoneIds);
        }
        if (agv.AgvStatus?.ZoneIds?.Count == 0)
        {
            await UpdateOfflineErrors(context, agv, handleResourceErrors: false);
        }
    }
}