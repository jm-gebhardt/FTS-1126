using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides a handler for agv related commands.
/// </summary>
[UsedImplicitly]
public class AgvCommandHandler :
    IConsumer<TryReactivateChargingAgv>,
    IConsumer<UpdateErrorStatus>,
    IConsumer<CreateAgv>,
    IConsumer<StartCharging>,
    IConsumer<StopCharging>,
    IConsumer<UpdateAgvStatus>,
    IConsumer<UpdateGraph>,
    IConsumer<GoOffline>,
    IConsumer<UpdateAgvData>,
    IConsumer<LockAgv>,
    IConsumer<UnlockAgv>,
    IConsumer<AcceptAgvCommand>,
    IConsumer<MoveAgv>,
    IConsumer<SetAgvForceIdle>,
    IConsumer<HandleAgvActionStartFailure>,
    IConsumer<HandleActionCompletion>,
    IConsumer<PrepareAgvForStartingAction>
{
    private readonly ILogger<AgvCommandHandler> _logger;
    private readonly IActionManagerSemaphoreProvider _actionManagerSemaphoreProvider;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly IAgvRepository _agvRepository;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;
    private readonly IMapper _mapper;
    private readonly IAgvGraphRegistry _agvGraphRegistry;
    private readonly IMapRegistry _mapRegistry;
    private readonly ProductionOptions _productionOptions;
    private readonly IAgvDataRegistry _agvDataRegistry;

    /// <summary>
    /// Initializes a new <see cref="AgvCommandHandler"/> instance.
    /// </summary>
    public AgvCommandHandler(IActionManagerSemaphoreProvider actionManagerSemaphoreProvider,
        IAgvRepository agvRepository,
        ISettingsProvider<AgvSettings> settingsProvider,
        IActionGroupRegistry actionGroupRegistry,
        IAgvDataRegistry agvDataRegistry,
        IAgvGraphRegistry agvGraphRegistry,
        IMapRegistry mapRegistry,
        IMapper mapper,
        ProductionOptions productionOptions,
        ILogger<AgvCommandHandler> logger)
    {
        _logger = logger;
        _actionManagerSemaphoreProvider = actionManagerSemaphoreProvider;
        _actionGroupRegistry = actionGroupRegistry;
        _agvRepository = agvRepository;
        _settingsProvider = settingsProvider;
        _mapper = mapper;
        _agvGraphRegistry = agvGraphRegistry;
        _mapRegistry = mapRegistry;
        _productionOptions = productionOptions;
        _agvDataRegistry = agvDataRegistry;
    }

    /// <summary>
    /// Handles the <see cref="TryReactivateChargingAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryReactivateChargingAgv> context)
    {
        var cancellationToken = context.CancellationToken;
        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var agvSettings = _settingsProvider.GetSettings();
            ICollection<Domain.Models.Agv.Agv> goodAgvs = await _agvRepository.GetChargingAgvsWithGoodBatteryLevelAsync(agvSettings.BatteryLevelGood);

            // preliminary logic: just go through all potential available Agvs and try to start a new action
            foreach (var agv in goodAgvs.OrderByDescending(a => a.AgvStatus!.BatteryCharge))
            {
                if (await _actionGroupRegistry.AgvHasPendingActions(agv.CorrelationId))
                {
                    _logger.LogDebug("There is a new action for the charging Agv with CorrelationId: {AgvCorrelationId}. So stop charging by setting it to idle, so that it takes the new Action", agv.CorrelationId);
                    agv.TransitionToIdle();
                    await _agvRepository.UpdateAgvAsync(agv);
                    await context.Publish(new AgvBecameIdle(agv.CorrelationId), cancellationToken);
                    await TryPublishStateChange(context, agv);
                    // only do this for the Agv with the best battery level and quit
                    return;
                }

                _logger.LogDebug("No action needed for charging Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="UpdateErrorStatus"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateErrorStatus> context)
    {
        (var correlationId, List<AgvError> errors) = context.Message;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        bool agvWasOk = !agv.HasError;

        agv.HasError = errors.Any();
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(UpdateErrorStatus), agv.CorrelationId);
            return;
        }
        if (agvWasOk && agv.HasError)
        {
            var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
            await context.Publish(new AgvPropertyChanged(agvMessageContent, agv.CorrelationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="CreateAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateAgv> context)
    {
        (var correlationId, string agvId, var agvStatus) = context.Message;

        _logger.LogDebug("Creating agv {CorrelationId}, {AgvId}", correlationId, agvId);

        var agv = new Domain.Models.Agv.Agv(correlationId, agvId, _mapper.Map<AgvStatus>(agvStatus));
        bool graphExists = await _agvGraphRegistry.ExistsAsync(correlationId);
        if (!graphExists)
        {
            await PublishGraphRequest(context, agv);
        }

        agv.TransitionToDiscovered(graphExists);
        agv.AgvDataRequestedLastTime = DateTime.UtcNow;
        await _agvRepository.AddAgvAsync(agv);
        await PublishDataRequest(context, agv);
        await TryPublishStateChange(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="StartCharging"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartCharging> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        agv.TransitionToCharging();

        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(StartCharging), agv.CorrelationId);
            return;
        }
        _logger.LogInformation("{AgvId} / {CorrelationId} has started charging (BatteryLevel: {BatteryLevel}%) => Transitioned to {State}",
            agv.AgvId, agv.CorrelationId, agv.AgvStatus!.BatteryCharge, Enum.GetName(agv.CurrentAgvState));
        await TryPublishStateChange(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="StopCharging"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StopCharging> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        agv.TransitionToIdle();
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(StopCharging), agv.CorrelationId);
            return;
        }

        _logger.LogInformation("{AgvId} / {CorrelationId} reloaded its battery level to {BatteryLevel}% => Transitioned to {State}",
            agv.AgvId, agv.CorrelationId, agv.AgvStatus!.BatteryCharge, Enum.GetName(agv.CurrentAgvState));
        await context.Publish(new AgvBecameIdle(correlationId));
        await TryPublishStateChange(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvStatus"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvStatus> context)
    {
        (var correlationId, string _, var agvStatusMessageContent) = context.Message;
        var agvStatus = _mapper.Map<AgvStatus>(agvStatusMessageContent);
        var agv = await _agvRepository.GetAgvAsync(correlationId);
        const bool newAlive = true;

        _logger.LogDebug("Updating AgvStatus for agv {CorrelationId}, {OldStatus} -> {NewStatus}", correlationId, agv.AgvStatus?.ToString(), agvStatus.ToString());
        var oldStatus = agv.AgvStatus;
        bool stateChanged = !Equals(oldStatus, agvStatus);
        bool aliveChanged = agv.IsOnline != newAlive;

        await ResetOfflineWatchDog(context, agv);

        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(agvStatus);

        if (agv.ShouldRequestGraph())
        {
            await PublishGraphRequest(context, agv);
        }

        if (agv.ShouldRequestData())
        {
            await PublishDataRequest(context, agv);
        }

        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(UpdateAgvStatus), agv.CorrelationId);
            return;
        }

        await PublishAgvStatusUpdateEvents(context, agv, batteryChargeChanged, oldStatus, lastNodeIdChanged, aliveChanged, stateChanged);

        await TryAutoAccept(context, agv);

        await TryPublishStateChange(context, agv);
    }

    private async Task ResetOfflineWatchDog(ConsumeContext context, Domain.Models.Agv.Agv agv)
    {
        if (agv.AgvOfflineTokenId != null)
        {
            var scheduler = context.GetPayload<MessageSchedulerContext>();
            await scheduler.CancelScheduledPublish<AgvWentOffline>(agv.AgvOfflineTokenId.Value);
        }

        var agvSettings = _settingsProvider.GetSettings();
        int delay = agv.CurrentAgvState == AgvState.Busy ? agvSettings.AgvOfflineTimeoutMsWhileBusy : agvSettings.AgvOfflineTimeoutMsWhileIdle;
        var creationTimeStamp = DateTime.UtcNow;
        ScheduledMessage<AgvWentOffline> delayedMessage = await context.SchedulePublish(creationTimeStamp.AddMilliseconds(delay), new AgvWentOffline(agv.CorrelationId, creationTimeStamp));
        agv.SetOnline(delayedMessage.TokenId);
    }

    /// <summary>
    /// Handles the <see cref="HandleActionCompletion"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleActionCompletion> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been completed and type is not charge", correlationId);

        var agv = await _agvRepository.GetAgvAsync(correlationId);
        agv.TransitionToIdle();
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(HandleActionCompletion), agv.CorrelationId);
        }

        await context.Publish(new AgvBecameIdle(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="UpdateGraph"/>
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateGraph> context)
    {
        var (correlationId, agvGraph) = context.Message;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        _logger.LogDebug("UpdateGraph for agv {CorrelationId}", agv.CorrelationId);
        agv.GraphExists = true;

        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(UpdateGraph), agv.CorrelationId);
            return;
        }
        if (!_mapRegistry.Exists(agvGraph.RelatedMapId))
        {
            await context.Publish(new RequestAgvMap(correlationId, agv.AgvId, agvGraph.RelatedMapId, agvGraph.RelatedMapName));
        }
    }

    /// <summary>
    /// Handles the <see cref="GoOffline"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<GoOffline> context)
    {
        var (correlationId, offlineTokenId) = context.Message;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        bool wasOnline = agv.IsOnline;
        agv.GoOffline(offlineTokenId);
        if (wasOnline && !agv.IsOnline)
        {
            if (!await _agvRepository.UpdateAgvAsync(agv))
            {
                _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(GoOffline), agv.CorrelationId);
                return;
            }
            var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
            await context.Publish(new AgvPropertyChanged(agvMessageContent, agv.CorrelationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvData"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvData> context)
    {
        var (correlationId, agvData) = context.Message;
        var agv = await _agvRepository.GetAgvAsync(correlationId);
        if (!await _agvDataRegistry.AddOrUpdateAsync(agvData))
        {
            _logger.LogWarning("Failed to store AgvData for {AgvId} / {CorrelationId}", agv.AgvId, correlationId);
            return;
        }
        agv.DataExist = true;
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(UpdateAgvData), agv.CorrelationId);
        }
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
        agv.Lock();
        _logger.LogInformation("Agv {correlationId} was locked.", agvCorrelationId);
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(LockAgv), agv.CorrelationId);
        }
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvPropertyChanged(agvMessageContent, agv.CorrelationId));
        await context.RespondAsync(new LockAgvResult(agv.CorrelationId));
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
        _logger.LogInformation("Agv {correlationId} was unlocked.", agvCorrelationId);
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(UnlockAgv), agv.CorrelationId);
        }
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvPropertyChanged(agvMessageContent, agv.CorrelationId));
        await context.Publish(new AgvBecameIdle(agvCorrelationId));
        await TryPublishStateChange(context, agv);
        await context.RespondAsync(new UnlockAgvResult(agv.CorrelationId));
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

        bool accepted = agv.TryAccept();
        if (!accepted)
        {
            _logger.LogDebug("Could not transition {AgvId} / {CorrelationId} to idle in state {State}", agv.AgvId, agv.CorrelationId, Enum.GetName(agv.CurrentAgvState));
            await context.RespondAsync(new AcceptAgvRejected(agv.CorrelationId));
            return;
        }
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(AcceptAgvCommand), agv.CorrelationId);
        }
        _logger.LogDebug("AGV {AgvId} / {CorrelationId} was set to idle", agv.AgvId, agv.CorrelationId);
        await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        await TryPublishStateChange(context, agv);
        await context.RespondAsync(new AcceptAgvSucceeded(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="MoveAgv"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<MoveAgv> context)
    {
        (var correlationId, string targetNodeId, var creator) = context.Message;

        var agv = await TryGetAgv(correlationId);
        if (agv == null)
        {
            await context.RespondAsync(new AgvNotFoundResult(correlationId));
            return;
        }

        var groupCorrelationId = Guid.NewGuid();
        _logger.LogInformation("{MessageType} for AgvCorrelationId: {AgvCorrelationId} to destinationNodeId:{DestinationNodeId} received. Sending QueueActionGroup {ActionGroupCorrelationId}", nameof(MoveAgv), context.Message.CorrelationId, context.Message.TargetNodeId, groupCorrelationId);

        var moveActionGroup = new ActionGroup(
            groupCorrelationId,
            $"Manual {ActionType.Move}",
            new List<AgvAction> { new(Guid.NewGuid(), ActionType.Move, targetNodeId, groupCorrelationId) },
            assignedAgv: agv.CorrelationId,
            isManual: true,
            creator: creator
        );

        await context.Publish(new QueueActionGroup(moveActionGroup, true));
        await context.RespondAsync(new MoveAgvResult(moveActionGroup.CorrelationId));
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
        agv.ForceIdle();
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(SetAgvForceIdle), agv.CorrelationId);
            return;
        }
        _logger.LogWarning("AGV {AgvId} / {CorrelationId} was set to idle by force", agv.AgvId, agv.CorrelationId);
        await context.Publish(new AgvBecameIdle(correlationId));
        await TryPublishStateChange(context, agv);
        await context.RespondAsync(new SetAgvForceIdleResult(correlationId));
    }

    /// <summary>
    /// Handles the <see cref="HandleAgvActionStartFailure"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleAgvActionStartFailure> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);
        agv.TransitionToIdle();
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(HandleAgvActionStartFailure), agv.CorrelationId);
            return;
        }
        await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        await TryPublishStateChange(context, agv);
    }

    /// <summary>
    /// Handles the <see cref="PrepareAgvForStartingAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PrepareAgvForStartingAction> context)
    {
        var (action, agvCorrelationId) = context.Message;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
        agv.TryTransitionToBusy(true);
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId);
            return;
        }

        await context.Publish(new AgvPreparedForStartingAction(action, agvCorrelationId));
    }

    private async Task<Domain.Models.Agv.Agv?> TryGetAgv(Guid agvCorrelationId)
    {
        Domain.Models.Agv.Agv? agv = null;
        try
        {
            agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
        }
        catch (AgvNotFoundException)
        {
            _logger.LogWarning("Agv {Agv} not found for responding to request.", agvCorrelationId);
        }
        return agv;
    }

    private async Task TryAutoAccept(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        _logger.LogDebug("AutoAccept {CorrelationId}", agv.CorrelationId);
        if (_productionOptions.AutoAccept && agv is { GraphExists: true, DataExist: true, CurrentAgvState: AgvState.Discovered })
        {
            var graph = await _agvGraphRegistry.GetAgvGraphAsync(agv.CorrelationId);
            bool mapExists = _mapRegistry.Exists(graph.RelatedMapId);
            _logger.LogDebug("AutoAccept {CorrelationId}, Graph: {GraphExists}, Data: {DataExists}, Map: {MapExists}", agv.CorrelationId, agv.GraphExists, agv.DataExist, mapExists);
            if (mapExists)
            {
                agv.TransitionToIdle();
                await _agvRepository.UpdateAgvAsync(agv);
                await context.Publish(new AgvBecameIdle(agv.CorrelationId));
            }
        }
    }

    private async Task TryPublishStateChange(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        if (agv.StateChanged)
        {
            await context.Publish(new AgvStateChanged(agv.CorrelationId, _mapper.Map<Messages.MessageContent.Agv>(agv), agv.PreviousAgvState));
        }
    }

    private async Task PublishGraphRequest(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        _logger.LogDebug("Requesting graph for agv {CorrelationId}", agv.CorrelationId);
        await context.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvGraph(agv.CorrelationId, agv.AgvId));
        agv.AgvGraphRequestedLastTime = DateTime.UtcNow;
    }

    private async Task PublishDataRequest(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        _logger.LogDebug("Requesting data for agv {CorrelationId}", agv.CorrelationId);
        await context.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvData(agv.CorrelationId, agv.AgvId));
        agv.AgvDataRequestedLastTime = DateTime.UtcNow;
    }

    private async Task PublishAgvStatusUpdateEvents(IPublishEndpoint context, Domain.Models.Agv.Agv agv, bool batteryChargeChanged, AgvStatus? oldStatus, bool lastNodeIdChanged, bool aliveChanged, bool stateChanged)
    {
        if (batteryChargeChanged)
        {
            await context.Publish(new AgvBatteryChargeChanged(agv.CorrelationId, oldStatus!.BatteryCharge, agv.AgvStatus!.BatteryCharge));
        }
        if (lastNodeIdChanged)
        {
            await context.Publish(new AgvLastNodeIdChanged(agv.CorrelationId, oldStatus!.LastNodeId, agv.AgvStatus!.LastNodeId));
        }
        if (aliveChanged || stateChanged)
        {
            var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
            await context.Publish(new AgvPropertyChanged(agvMessageContent, agv.CorrelationId));
        }
        if (aliveChanged && agv.CurrentAgvState == AgvState.Idle) // Transition to Ready must happen before triggering order assignment
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        }
    }
}