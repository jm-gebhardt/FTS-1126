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
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;
using System.Collections.ObjectModel;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Zones;
using Microsoft.IdentityModel.Tokens;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using Agv = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Agv;
using AgvSettings = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvSettings;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvGraph;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;
using CreateQueueingActionGroup = Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup.CreateQueueingActionGroup;
using RequestAgvZones = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvZones;
using UpdateErrorStatus = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UpdateErrorStatus;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;

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
    IConsumer<PrepareParkingOfAgv>,
    IConsumer<PrepareChargingOfAgv>,
    IConsumer<UpdateAgvSettingsGroupReference>,
    IConsumer<TryReactivateParkingAgvs>,
    IConsumer<SwitchMode>,
    IConsumer<TryEnqueueAgv>,
    IConsumer<ReceiveAgvZones>,
    IConsumer<RemoveAgvFromZones>,
    IConsumer<LeaveQueue>
{
    private readonly ILogger<AgvCommandHandler> _logger;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _agvsSemaphore;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IMapper _mapper;
    private readonly ProductionOptions _productionOptions;
    private readonly IAgvDb _agvDb;
    private readonly IParkPointService _parkPointService;
    private readonly IQueueManager _queueManager;
    private readonly INodeStateRepository _nodeStateRepository;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IMetricCollectionService _metricCollectionService;
    private readonly IStationRepository _stationRepository;
    private readonly ISemaphoreProvider<IQueueingSemaphore> _queueSemaphoreProvider;

    /// <inheritdoc cref="AgvCommandHandler"/>
    public AgvCommandHandler(ISemaphoreProvider<IAgvsSemaphore> agvsSemaphore,
        IAgvDb agvDb,
        IActionGroupRepository actionGroupRepository,
        IParkPointService parkPointService,
        IQueueManager queueManager,
        IChargingPointManager chargingPointManager,
        INodeStateRepository nodeStateRepository,
        IMetricCollectionService metricCollectionService,
        IMapper mapper,
        ProductionOptions productionOptions,
        IStationRepository stationRepository,
        ISemaphoreProvider<IQueueingSemaphore> queueSemaphoreProvider,
        ILogger<AgvCommandHandler> logger
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
        _metricCollectionService = metricCollectionService;
        _productionOptions = productionOptions;
        _chargingPointManager = chargingPointManager;
        _stationRepository = stationRepository;
        _queueSemaphoreProvider = queueSemaphoreProvider;
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
            _logger.LogInformation("AGV:{CorrelationId} is locked. Do not prepare for charging.", agv.CorrelationId);
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
            _logger.LogInformation("Did not prepare AGV:{CorrelationID} for charging. AGV has {Count} action groups that are not started and the following battery charge: {BatteryCharge}.",
                agv.CorrelationId,
                actionGroups.Count,
                agv.AgvStatus!.BatteryCharge);
            return;
        }

        if (await _chargingPointManager.IsOnAChargingNodeAsync(agv))
        {
            _logger.LogInformation("Agv {AgvCorrelationId} is already on charger {NodeId}, no need to reserve one", agv.CorrelationId, agv.AgvStatus?.LastNodeId);
            await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, agv.AgvStatus!.LastNodeId!, initiator));
            return;
        }

        var reservedChargingNode = await _chargingPointManager.GetReservedChargerForAgvAsync(agvCorrelationId);
        string nodeIdToPublish = reservedChargingNode?.NodeId ?? (await _chargingPointManager.ReserveChargerAsync(agv)).Id;

        await PublishNodeStateChange(context, nodeIdToPublish);
        await context.Publish(new ChargingNodeSelectedForAgv(agv.CorrelationId, nodeIdToPublish, initiator));
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

        if (await _actionGroupRepository.HasCurrentAssignedActionGroupAsync(agvCorrelationId))
        {
            _logger.LogDebug("No need for a park / queue action because the agv {AgvCorrelationId} is already assigned to an action group", agvCorrelationId);
            await context.Publish(new AgvBecameIdle(agvCorrelationId));
            return;
        }

        bool hasParkers = await  _parkPointService.HasParkersAsync(agvCorrelationId);
        if (hasParkers && (await _parkPointService.IsOnAParkNodeAsync(agv) || await _parkPointService.AgvHasReservedParkingNodeAsync(agvCorrelationId)))
        {
            _logger.LogDebug("No need for a park action because the agv is already in parking for agvId: {AgvCorrelationId}", agvCorrelationId);
            return;
        }

        if (agv.IsQueueing || await _queueManager.IsOnAQueueNode(agvCorrelationId))
        {
            _logger.LogDebug("No need for a queue action because the agv is already in queuing for agvId: {AgvCorrelationId}", agvCorrelationId);
            return;
        }

        string? targetNode = await _queueManager.TryEnterQueue(agv);
        if (targetNode != null)
        {
            agv.IsQueueing = true;
            await _agvDb.Agvs.UpdateAgvAsync(agv);
            _logger.LogInformation("Starting queueing action for agv with CorrelationId: {AgvCorrelationId}.", agvCorrelationId);
            await context.Publish(new QueueNodeSelectedForAgv(agvCorrelationId, targetNode));
            return;
        }

        if (hasParkers)
        {
            _logger.LogInformation("Starting park action for agv with CorrelationId: {AgvCorrelationId}", agvCorrelationId);
            var parkNode = await _parkPointService.ReserveParkAsync(agv);
            await PublishNodeStateChange(context, parkNode.Id);
            await context.Publish(new ParkingNodeSelectedForAgv(agvCorrelationId, parkNode.Id));
        }
        else
        {
            _logger.LogInformation("No parking stations defined");
        }
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
            zoneIdsChanged = !Equals(oldStatus?.ZoneIds, agvStatus.ZoneIds);
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
        }
        bool wasReady = agv.IsReady;
        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(agvStatus);
        bool isReady = agv.IsReady;

        LogBatteryStates(oldStatus, agv.AgvStatus, agvSettings);
        _metricCollectionService.CollectBatteryChargeMetrics(agvStatus);

        if (!wasReady && isReady)
        {
            _logger.LogAgvInformation(LogCodes.AgvReady, correlationId, "AGV Ready",
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

        if (zoneIdsChanged && agv.IsOnline == wasOnline)
        {
            await UpdateOfflineErrors(context, agv, handleRessourceErrors: false);
        }

        if (zoneIdsChanged || batteryChargeChanged || lastNodeIdChanged || isReady != wasReady || agv.IsOnline != wasOnline || wasDisturbed != agvStatus.Disturbed || wasAutomatic != agvStatus.Automatic)
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
                    await context.Publish(new AgvWentOnline());
                }
            }
        }
        await PublishAgvStatusUpdateEvents(context, agv, batteryChargeChanged, oldStatus, lastNodeIdChanged, wasOnline, agvSettings);

        await TryAutoAccept(context, agv, context.CancellationToken);
    }

    private async Task PublishAgvStatusUpdateEvents(IPublishEndpoint context,
        Agv agv,
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
            await PublishLastNodeIdChange(context, agv, oldStatus!.LastNodeId, agvStatus.LastNodeId);
        }
        bool isIdle = agv.CurrentAgvState == AgvState.Idle && !agvStatus.Busy;
        if (isIdle && oldStatus!.Busy)
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        }
        if (!wasOnline && agv.IsOnline)
        {
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, true));
            _logger.LogAgvInformation(LogCodes.AgvOnline, agvStatus.CorrelationId, "AGV Online",
                $"AGV {agv.SerialNumber} is online",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });

            if (isIdle)
            {
                await context.Publish(new AgvBecameIdle(agv.CorrelationId));
            }
        }
    }

    private async Task PublishLastNodeIdChange(IPublishEndpoint context, Agv agv, string? oldLastNodeId, string? newLastNodeId)
    {
        await context.Publish(new AgvLastNodeIdChanged(agv.CorrelationId, oldLastNodeId, newLastNodeId));
        Station? station;
        if (newLastNodeId != null)
        {
            station = await _stationRepository.GetRequiredStationBaseInformation(newLastNodeId);
            if (station != null)
            {
                _logger.LogStationInformation(LogCodes.StationAgvReached, station.Id, "AGV Reached Station",
                    $"AGV {agv.SerialNumber} reached station",
                    new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
            }
        }
        if (oldLastNodeId == null)
        {
            return;
        }

        station = await _stationRepository.GetRequiredStationBaseInformation(oldLastNodeId);
        if (station != null)
        {
            _logger.LogStationInformation(LogCodes.StationAgvLeft, station.Id, "AGV Left Station",
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
        _logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been completed and type is not charge", correlationId);

        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        agv.IsQueueing = context.Message.IsQueueing;
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

        _logger.LogAgvInformation(LogCodes.AgvGraphReceived, correlationId, "AGV Graph Received",
            $"AGV {agv.SerialNumber} graph has been received", new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        await _agvDb.Agvs.UpdateAgvAsync(agv);
        if (await _agvDb.Agvs.HasOnlineAgvs())
        {
            await context.Publish(new AgvWentOnline());
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
            await UpdateOfflineErrors(context, agv);
            if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
            {
                _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(GoOffline), agv.CorrelationId);
                return;
            }
            await context.Publish(new ClearNodesFromAgv(agv.CorrelationId));
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, false));
            _logger.LogAgvInformation(LogCodes.AgvOffline, agv.CorrelationId, "AGV Offline",
                $"AGV {agv.SerialNumber} is offline",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        }

        if (!await _agvDb.Agvs.HasOnlineAgvs())
        {
            _logger.LogWarning("All agvs went offline");
            await context.Publish(new AllAgvsOffline());
        }
    }

    private async Task UpdateOfflineErrors(IPublishEndpoint context, Agv agv, bool handleZoneErrors = true, bool handleRessourceErrors = true)
    {
        List<AgvError> agvErrors = (await _agvDb.Errors.GetActiveErrorsAsync(agv.CorrelationId)).ToList();
        var position = _agvDb.Positions.GetPosition(agv.CorrelationId);

        if (handleZoneErrors)
        {
            await UpdateZoneErrors(context, agv, agvErrors, position);
        }

        if (handleRessourceErrors)
        {
            await UpdateRessourceBlockingErrors(context, agv, agvErrors, position);
        }
    }

    private async Task UpdateRessourceBlockingErrors(IPublishEndpoint context, Agv agv, List<AgvError> agvErrors, AgvPositionStatus? position)
    {
        bool hasRessourceErrors = agvErrors.Any(e => e.IsRessourceError());

        // Remove RessourceBlockingError from agv errors
        if (agv.IsOnline && hasRessourceErrors)
        {
            _logger.LogInformation("Removing ressource blocking errors for agv {AgvCorrelationId}", agv.CorrelationId);
            agvErrors.RemoveAll(e => e.IsRessourceError());
            await context.Publish(new UpdateErrorStatus(agv.CorrelationId, agvErrors.ToList(), position!.AgvPosition));
            return;
        }

        // Add RessourceBlockingError(s)
        ICollection<NodeLock> nodeLocks = await _nodeStateRepository.GetNodeLocksForAgv(agv.CorrelationId);
        if (!hasRessourceErrors && !agv.IsOnline && nodeLocks.Count > 0)
        {
            _logger.LogInformation("Adding ressource blocking errors for agv {AgvCorrelationId}", agv.CorrelationId);
            foreach (var nodeLock in nodeLocks)
            {
                agvErrors.Add(new RessourceBlockingError(agv.CorrelationId, DateTime.UtcNow, nodeLock.NodeId));
            }
            await context.Publish(new UpdateErrorStatus(agv.CorrelationId, agvErrors.ToList(), position!.AgvPosition));
        }
    }

    private async Task UpdateZoneErrors(IPublishEndpoint context, Agv agv, List<AgvError> agvErrors, AgvPositionStatus? position)
    {
        bool hasZoneErrors = agvErrors.Any(e => e.IsZoneError());
        bool hasZonesInStatus = !agv.AgvStatus!.ZoneIds.IsNullOrEmpty();

        // Add ZoneError(s) to agv errors if it is not online
        if (hasZonesInStatus && !hasZoneErrors && !agv.IsOnline)
        {
            _logger.LogInformation("Adding zone errors for agv {AgvCorrelationId}", agv.CorrelationId);
            ICollection<Zone> zones = await _agvDb.Zones.GetZones(agv.AgvStatus!.ZoneIds!.ToArray());
            foreach (var zone in zones)
            {
                agvErrors.Add(new ZoneError(agv.CorrelationId, DateTime.UtcNow, zone.ZoneId));
            }
            await context.Publish(new UpdateErrorStatus(agv.CorrelationId, agvErrors.ToList(), position!.AgvPosition));
            return;
        }

        // Remove ZoneError(s) from agv errors
        if ((agv.IsOnline || !hasZonesInStatus) && hasZoneErrors)
        {
            _logger.LogInformation("Removing zone errors for agv {AgvCorrelationId}", agv.CorrelationId);
            agvErrors.RemoveAll(e => e.IsZoneError());
            await context.Publish(new UpdateErrorStatus(agv.CorrelationId, agvErrors.ToList(), position!.AgvPosition));
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
        _logger.LogAgvInformation(LogCodes.AgvInformationReceived, correlationId, "AGV Information Received",
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
        _logger.LogAgvInformation(LogCodes.AgvModeChangePending, agv.CorrelationId, "AGV Mode Change Pending",
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

        agv.SetNormalMode(initiatingUser);

        await _agvDb.Agvs.UpdateAgvAsync(agv);
        _logger.LogAgvInformation(LogCodes.AgvModeChanged, agv.CorrelationId, "AGV Mode Changed",
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
            _logger.LogAgvInformation(LogCodes.AgvUnlocked, agv.CorrelationId, "AGV Unlocked",
                $"AGV {agv.SerialNumber} is unlocked",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
            await context.Publish(new AgvLockedChanged(agv.CorrelationId, false));
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
        _logger.LogAgvInformation(LogCodes.AgvModeChangePending, agv.CorrelationId, "AGV Mode Change Pending",
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
        _logger.LogAgvInformation(LogCodes.AgvModeChangePending, agv.CorrelationId, "AGV Mode Change Pending",
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
            _logger.LogAgvInformation(LogCodes.AgvLocked, agv.CorrelationId, "AGV Locked", $"AGV {agv.SerialNumber} is locked",
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
            _logger.LogAgvInformation(LogCodes.AgvModeChanged, agv.CorrelationId, "AGV Mode Changed", $"AGV {agv.SerialNumber} set to mode '{agv.Mode.Type.ToString()}'",
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
        _logger.LogAgvInformation(LogCodes.AgvUnlocked, agv.CorrelationId, "AGV Unlocked",
            $"AGV {agv.SerialNumber} is unlocked",
            new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });

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
            _logger.LogError("{Command}: Could not update agv {AgvCorrelationId}", nameof(SetAgvForceIdle), agv.CorrelationId);
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
            _logger.LogInformation("Agv {AgvCorrelationId} is not available for busy", agv.CorrelationId);
            return;
        }

        var actionGroup = await _actionGroupRepository.GetActionGroupAsync(action.ParentId);
        if (actionGroup == null)
        {
            _logger.LogError("ActionGroup {ActionGroupCorrelationId} not found during preparation of agv {AgvCorrelationId} for starting", action.ParentId, agv.CorrelationId);
            return;
        }

        if (actionGroup.Actions.Any(a => a.ActionType == ActionType.Charge) || actionGroup.IsManual)
        {
            await _queueSemaphoreProvider.ExecuteLockedAsync(async () =>
            {
                if (await TryEscapeQueue(context, agv.CorrelationId, agv.AgvStatus?.LastNodeId))
                {
                    agv.IsQueueing = false;
                }

                if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
                {
                    _logger.LogInformation("{Command}: Could not update agv {AgvCorrelationId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId);
                    return;
                }
                _logger.LogInformation("{Command}: Prepared agv {AgvCorrelationId} for starting action {ActionId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId, action.CorrelationId);
                await PublishStateChange(context, agv);
                await context.Publish(new AgvPreparedForStartingAction(agvCorrelationId, action));
            });
            return;
        }

        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogInformation("{Command}: Could not update agv {AgvCorrelationId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId);
            return;
        }
        await _queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await TryDequeueAgv(context, agv.CorrelationId);
            _logger.LogInformation("{Command}: Prepared agv {AgvCorrelationId} for starting action {ActionId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId, action.CorrelationId);
            await PublishStateChange(context, agv);
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
            string cargoState = hasCargo ? "loaded" : "unloaded";
            _logger.LogAgvInformation(LogCodes.AgvCargoUpdated, agvCorrelationId, "Cargo Updated",
                $"AGV {agv.SerialNumber} cargo has changed to {cargoState} ",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber }, { LogLabels.CargoState, cargoState } });
        }
    }

    private async Task TryDequeueAgv(IPublishEndpoint context, Guid agvCorrelationId)
    {
        Dictionary<Guid, string>? agvsToMove = await _queueManager.TryDequeueAgv(agvCorrelationId);
        if (agvsToMove == null)
        {
            return;
        }
        if (agvsToMove.Count == 0)
        {
            await context.Publish(new QueueBecameEmpty());
        }
        foreach (KeyValuePair<Guid, string> agvToMove in agvsToMove)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(agvToMove.Key);
            if (agv.IsQueueing)
            {
                _logger.LogInformation("Trigger creating queue action for agv {AgvCorrelationId} to node {NodeId}", agvToMove.Key, agvToMove.Value);
                await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvToMove.Key, agvToMove.Value));
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
        (var agvCorrelationId, string entryNode) = context.Message;

        await _queueSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            string? targetNode = await _queueManager.TryEnqueueAgv(agvCorrelationId, entryNode);
            if (targetNode != null && targetNode != entryNode)
            {
                await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvCorrelationId, targetNode));
                await context.Publish(new QueueEntered());
            }

            if (targetNode == entryNode)
            {
                await context.Publish(new QueueEntered());
            }
        });
    }

    private async Task<bool> TryEscapeQueue(IPublishEndpoint context, Guid agvCorrelationId, string? agvLastNodeId)
    {
        (Dictionary<Guid, string>? agvsToMove, int? queueSize) = await _queueManager.TryEscapeQueue(agvCorrelationId, agvLastNodeId);
        if (agvsToMove == null)
        {
            return false;
        }
        if (queueSize == 0)
        {
            await context.Publish(new QueueBecameEmpty());
        }
        foreach (KeyValuePair<Guid, string> agvToMove in agvsToMove)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(agvToMove.Key);
            if (agv.IsQueueing)
            {
                _logger.LogInformation("Trigger creating queue action for agv {AgvCorrelationId} to node {NodeId}", agvToMove.Key, agvToMove.Value);
                await context.Publish(new CreateQueueingActionGroup(Guid.NewGuid(), agvToMove.Key, agvToMove.Value));
            }
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

        foreach (var agv in agvsAtQueueHeads)
        {
            await context.Publish(new AgvBecameIdle(agv));
        }
        foreach (var agv in readyAgvs)
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
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
    /// Handles the <see cref="RemoveAgvFromZones"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromZones> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        agv.AgvStatus!.ResetZones();
        await UpdateOfflineErrors(context, agv, handleRessourceErrors: false);
        await _agvDb.Agvs.UpdateAgvAsync(agv);
    }

    private async Task<Agv?> TryGetAgv(Guid agvCorrelationId)
    {
        var agv = await _agvDb.Agvs.TryGetAgvAsync(agvCorrelationId);
        if (agv == null)
        {
            _logger.LogWarning("Agv {AgvCorrelationId} not found for responding to request.", agvCorrelationId);
        }
        return agv;
    }

    private async Task TryAutoAccept(IPublishEndpoint context, Agv agv, CancellationToken cancellationToken)
    {
        _logger.LogDebug("AutoAccept {CorrelationId}", agv.CorrelationId);
        if (_productionOptions.AutoAccept && agv is { GraphExists: true, DataExist: true, CurrentAgvState: AgvState.Discovered })
        {
            _logger.LogDebug("AutoAccept {AgvCorrelationId}, Graph: {GraphExists}, Data: {DataExists}", agv.CorrelationId, agv.GraphExists, agv.DataExist);
            await TransitionToIdleAsync(context, agv, cancellationToken);
        }
    }

    private async Task TransitionToIdleAsync(IPublishEndpoint context, Agv agv, CancellationToken cancellationToken)
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
            _logger.LogAgvInformation(LogCodes.AgvIdle, agv.CorrelationId, "AGV Became Idle",
                $"AGV {agv.SerialNumber} became idle",
                new Dictionary<string, object?> { { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        }
    }

    private async Task PublishStateChange(IPublishEndpoint context, Agv agv)
    {
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvStateChanged(agv.CorrelationId, agv.CurrentAgvState, agv.PreviousAgvState, agvMessageContent));
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

        _logger.LogAgvInformation(LogCodes.AgvDiscovered, correlationId, "AGV Discovered", $"AGV {agvSerialNumber} has been discovered",
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

        bool wasCritical = oldSoc <= agvSettings.BatteryLevelCritical;
        bool isCritical = newSoc <= agvSettings.BatteryLevelCritical;

        bool isLow = newSoc <= agvSettings.BatteryLevelLow;
        bool wasLow = oldSoc <= agvSettings.BatteryLevelLow;

        if (wasCharging && !isCharging)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryFullyCharged, oldAgvStatus.CorrelationId,
                "Battery Fully Charged", $"Battery has finished charging at {Math.Round(newAgvStatus.BatteryCharge)}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, Math.Round(newAgvStatus.BatteryCharge) } });
        }

        if (!wasMaxChargedUp && isMaxChargedUp)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryMaxChargeLevel, oldAgvStatus.CorrelationId,
                "Maximum Charging Level", $"Battery has reached maximum charging level {agvSettings.MaxChargeUp}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, agvSettings.MaxChargeUp } });
        }

        if (!wasMinChargedUp && isMinChargedUp)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryMinChargeLevel, oldAgvStatus.CorrelationId,
                "Minimum Charging Level", $"Battery has reached minimum charging level {agvSettings.MinChargeUp}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, agvSettings.MinChargeUp } });
        }

        if (!wasCritical && isCritical)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryCriticalChargeLevel, oldAgvStatus.CorrelationId,
                "Critical Charging Level",
                $"Battery has reached critical charging level {agvSettings.BatteryLevelCritical}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, agvSettings.BatteryLevelCritical } });
        }

        if (!wasLow && isLow)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryLowChargeLevel, oldAgvStatus.CorrelationId,
                "Low Charging Level", $"Battery has reached low charging level {agvSettings.BatteryLevelLow}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, agvSettings.BatteryLevelLow } });
        }
        if (isCharging && !wasCharging)
        {
            _logger.LogBatteryInformation(LogCodes.BatteryStartedCharging, oldAgvStatus.CorrelationId,
                "Started Charging", $"Battery started charging at level {Math.Round(newAgvStatus.BatteryCharge)}%",
                new Dictionary<string, object?> { { LogLabels.BatteryLevel, Math.Round(newAgvStatus.BatteryCharge) } });
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
        if (await _agvDb.Graphs.TryGetNodeAsync(newStatus.LastNodeId) == null)
        {
            newStatus.UpdateLastNodeId(oldStatus.LastNodeId!);
        }
    }

    private async Task PrepareNodes(IPublishEndpoint context, AgvStatus agvStatus, Agv agv)
    {
        _logger.LogInformation("Preparing nodes for AGV {AgvCorrelationId}. Last node id of AGV is {LastNodeId}", agv.CorrelationId, agvStatus.LastNodeId);

        IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agv.CorrelationId);
        if (actionGroups.Count < 1 && agvStatus.LastNodeId == null)
        {
            return;
        }
        List<string> lockedNodes = (await _nodeStateRepository.GetNodeLocksForAgv(agv.CorrelationId)).Select(n => n.NodeId).ToList();

        List<string> currentNodeIds = actionGroups.SelectMany(a => a.Actions).Select(a => a.Target.NodeId).Where(t => t != null).Select(t => t!).ToList();
        if (agvStatus.LastNodeId != null)
        {
            currentNodeIds.Add(agvStatus.LastNodeId);
        }
        ICollection<GraphNode> currentNodes = (await _agvDb.Graphs.GetNodesAsync(n => currentNodeIds.Contains(n.Id))).ToList();
        IEnumerable<string> parkers = currentNodes.Where(t => t.IsParker()).Select(t => t.Id);

        var now = DateTime.UtcNow;
        foreach (var node in currentNodes.Where(n => n.NeedsReservation() && !lockedNodes.Contains(n.Id)))
        {
            var actionGroup = actionGroups.FirstOrDefault(a => a.Actions.Any(ac => ac.Target.NodeId == node.Id));
            if (actionGroup != null && actionGroup.IsOutdated(Agv.OfflineTolerance, now))
            {
                _logger.LogWarning("Action group {ActionGroupCorrelationId} outdated after agv {AgvCorrelationId} went back online", actionGroup.CorrelationId, agv.CorrelationId);
                await context.Publish(new CancelOrAbortActionGroup(actionGroup.CorrelationId));
                continue;
            }

            if (string.Equals(node.Id, agvStatus.LastNodeId))
            {
                _logger.LogInformation("Preparing nodes for AGV {AgvCorrelationId}. Occupy node {NodeId}", agv.CorrelationId, node.Id);

                await _nodeStateRepository.ForceOccupyNodeAsync(node.Id, agv.CorrelationId);
                await PublishNodeStateChange(context, node.Id);
                continue;
            }

            if (!await _nodeStateRepository.ReserveNodeAsync(node.Id, agv.CorrelationId))
            {
                if (actionGroup != null)
                {
                    _logger.LogWarning("Could not restore reservation of node {NodeId} for Agv {AgvCorrelationId}, aborting action group {ActionGroupCorrelationId}", node.Id, agv.CorrelationId, actionGroup.CorrelationId);
                    await context.Publish(new CancelOrAbortActionGroup(actionGroup.CorrelationId));
                    continue;
                }
                await PublishNodeStateChange(context, node.Id);
            }
        }

        foreach (var moveToQueue in actionGroups.Where(a => !parkers.Contains(a.Actions.First().Target.NodeId) && a is { IsMovingCargo: false, IsChargingOrder: false, IsManual: false }))
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
}