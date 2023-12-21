using System.Collections.ObjectModel;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvStatus = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.AgvStatus;
using Cargo = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Cargo;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides a handler for agv related commands.
/// </summary>
[UsedImplicitly]
public class AgvCommandHandler :
    IConsumer<TryReactivateChargingAgv>,
    IConsumer<UpdateErrorStatus>,
    IConsumer<StartCharging>,
    IConsumer<StopCharging>,
    IConsumer<UpdateAgvStatus>,
    IConsumer<SetAgvGraphExist>,
    IConsumer<GoOffline>,
    IConsumer<UpdateAgvData>,
    IConsumer<LockAgv>,
    IConsumer<UnlockAgv>,
    IConsumer<AcceptAgvCommand>,
    IConsumer<MoveAgv>,
    IConsumer<SetAgvForceIdle>,
    IConsumer<HandleAgvActionStartFailure>,
    IConsumer<HandleActionCompletion>,
    IConsumer<PrepareAgvForStartingAction>,
    IConsumer<UpdateCargo>
{
    private readonly ILogger<AgvCommandHandler> _logger;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _agvsSemaphore;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IAgvRepository _agvRepository;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;
    private readonly IMapper _mapper;
    private readonly IGraphRepository _graphRepository;
    private readonly ICargoRepository _cargoRepository;
    private readonly ProductionOptions _productionOptions;
    private readonly IAgvDataRepository _agvDataRepository;
    private readonly IRequestClient<QueueActionGroup> _queueActionGroupRequestClient;

    /// <inheritdoc cref="AgvCommandHandler"/>
    public AgvCommandHandler(ISemaphoreProvider<IAgvsSemaphore> agvsSemaphore,
        IAgvRepository agvRepository,
        ISettingsProvider<AgvSettings> settingsProvider,
        IActionGroupRepository actionGroupRepository,
        IAgvDataRepository agvDataRepository,
        IGraphRepository graphRepository,
        IRequestClient<QueueActionGroup> queueActionGroupRequestClient,
        ICargoRepository cargoRepository,
        IMapper mapper,
        ProductionOptions productionOptions,
        ILogger<AgvCommandHandler> logger)
    {
        _logger = logger;
        _agvsSemaphore = agvsSemaphore;
        _actionGroupRepository = actionGroupRepository;
        _agvRepository = agvRepository;
        _settingsProvider = settingsProvider;
        _mapper = mapper;
        _graphRepository = graphRepository;
        _cargoRepository = cargoRepository;
        _queueActionGroupRequestClient = queueActionGroupRequestClient;
        _productionOptions = productionOptions;
        _agvDataRepository = agvDataRepository;
    }

    /// <summary>
    /// Handles the <see cref="TryReactivateChargingAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryReactivateChargingAgv> context)
    {
        var cancellationToken = context.CancellationToken;
        await _agvsSemaphore.ExecuteLockedAsync(async () =>
        {
            var agvSettings = _settingsProvider.GetSettings();
            ICollection<Domain.Models.Agv.Agv> goodAgvs = await _agvRepository.GetChargingAgvsWithGoodBatteryLevelAsync(agvSettings.BatteryLevelGood);

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

                _logger.LogDebug("There is a new action for the charging Agv with CorrelationId: {AgvCorrelationId}. Abort charging action group: {ActionGroup}", agv.CorrelationId, action.ParentId);
                await context.Publish(new AbortActionGroup(action.ParentId), cancellationToken);
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
        (var correlationId, List<AgvError> errors) = context.Message;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        bool agvHasError = errors.Any();
        if (agvHasError != agv.HasError)
        {
            agv.HasError = agvHasError;
            await _agvRepository.UpdateAgvAsync(agv);
        }
    }

    /// <summary>
    /// Handles the <see cref="StartCharging"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartCharging> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        bool stateChanged = agv.TransitionToCharging();

        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(StartCharging), agv.CorrelationId);
            return;
        }
        _logger.LogInformation("{AgvId} / {CorrelationId} has started charging (BatteryLevel: {BatteryLevel}%) => Transitioned to {State}",
            agv.AgvId, agv.CorrelationId, agv.AgvStatus!.BatteryCharge, Enum.GetName(agv.CurrentAgvState));
        if (stateChanged)
        {
            await PublishStateChange(context, agv);
        }
    }

    /// <summary>
    /// Handles the <see cref="StopCharging"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StopCharging> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        await TransitionToIdleAsync(context, agv, context.CancellationToken);

        _logger.LogInformation("{AgvId} / {CorrelationId} reloaded its battery level to {BatteryLevel}% => Transitioned to {State}",
            agv.AgvId, agv.CorrelationId, agv.AgvStatus!.BatteryCharge, Enum.GetName(agv.CurrentAgvState));
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvStatus"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvStatus> context)
    {
        (var correlationId, string _, var agvStatusMessageContent) = context.Message;
        var agvStatus = _mapper.Map<AgvStatus>(agvStatusMessageContent);
        Domain.Models.Agv.Agv agv;
        try
        {
            agv = await _agvRepository.GetAgvAsync(correlationId);
        }
        catch (AgvNotFoundException)
        {
            await CreateAgv(context);
            return;
        }
        _logger.LogDebug("Updating AgvStatus for agv {CorrelationId}, {OldStatus} -> {NewStatus}", correlationId, agv.AgvStatus?.ToString(), agvStatus.ToString());
        var oldStatus = agv.AgvStatus;
        bool wasOnline = agv.IsOnline;

        await ResetOfflineWatchDog(context, agv);

        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(agvStatus);
        _logger.LogTrace("LastNodeIdChanged: {LastNodeIdChanged}, BatteryChargeChanged: {BatteryChargeChanged}", lastNodeIdChanged, batteryChargeChanged);

        // Only request AgvGraph if a global Graph exists
        if (agv.ShouldRequestGraph())
        {
            if (await _graphRepository.ExistsAsync())
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

        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(UpdateAgvStatus), agv.CorrelationId);
            return;
        }
        await PublishAgvStatusUpdateEvents(context, agv, batteryChargeChanged, oldStatus, lastNodeIdChanged, wasOnline);

        await TryAutoAccept(context, agv, context.CancellationToken);
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

    private async Task PublishAgvStatusUpdateEvents(IPublishEndpoint context,
        Domain.Models.Agv.Agv agv,
        bool batteryChargeChanged,
        AgvStatus? oldStatus,
        bool lastNodeIdChanged,
        bool wasOnline)
    {
        if (batteryChargeChanged)
        {
            await context.Publish(new AgvBatteryChargeChanged(agv.CorrelationId, oldStatus!.BatteryCharge, agv.AgvStatus!.BatteryCharge));
        }
        if (lastNodeIdChanged)
        {
            await context.Publish(new AgvLastNodeIdChanged(agv.CorrelationId, oldStatus!.LastNodeId, agv.AgvStatus!.LastNodeId));
        }
        if (agv.CurrentAgvState == AgvState.Idle && !agv.AgvStatus!.Busy && oldStatus!.Busy)
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        }
        if (!wasOnline && agv.IsOnline)
        {
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, true));
        }
    }

    /// <summary>
    /// Handles the <see cref="HandleActionCompletion"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HandleActionCompletion> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been completed and type is not charge", correlationId);

        var agv = await _agvRepository.GetAgvAsync(correlationId);
        await TransitionToIdleAsync(context, agv, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="SetAgvGraphExist"/>
    /// </summary>
    public async Task Consume(ConsumeContext<SetAgvGraphExist> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        _logger.LogDebug("UpdateGraph for agv {CorrelationId}", agv.CorrelationId);
        agv.GraphExists = true;

        await _agvRepository.UpdateAgvAsync(agv);
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
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, false));
        }
    }

    /// <summary>
    /// Handles the <see cref="UpdateAgvData"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateAgvData> context)
    {
        var (correlationId, agvData) = context.Message;
        var agv = await _agvRepository.GetAgvAsync(correlationId);
        if (!await _agvDataRepository.AddOrUpdateAsync(agvData))
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
        await context.Publish(new AgvLockedChanged(agv.CorrelationId, true));
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

        // publish also when state was already idle, to trigger further assignments
        _logger.LogInformation("Agv {correlationId} was unlocked.", agvCorrelationId);
        await context.Publish(new AgvBecameIdle(agv.CorrelationId), context.CancellationToken);
        await context.Publish(new AgvLockedChanged(agv.CorrelationId, false));
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

        (bool accepted, bool stateChanged) = agv.TryAccept();
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
        if (stateChanged)
        {
            await PublishStateChange(context, agv);
        }
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
            new List<AgvAction> {new(Guid.NewGuid(), ActionType.Move, targetNodeId, groupCorrelationId)},
            assignedAgv: agv.CorrelationId,
            isManual: true,
            creator: creator
        );

        await _queueActionGroupRequestClient.GetResponse<Success>(new QueueActionGroup(moveActionGroup, true));
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
        bool stateChanged = agv.ForceIdle();
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(SetAgvForceIdle), agv.CorrelationId);
            return;
        }
        _logger.LogWarning("AGV {AgvId} / {CorrelationId} was set to idle by force", agv.AgvId, agv.CorrelationId);
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
        var agv = await _agvRepository.GetAgvAsync(correlationId);
        await TransitionToIdleAsync(context, agv, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="PrepareAgvForStartingAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PrepareAgvForStartingAction> context)
    {
        var (action, agvCorrelationId) = context.Message;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
        bool isAvailable = agv.TryTransitionToBusy(true);

        if (!isAvailable)
        {
            _logger.LogWarning("Agv {CorrelationId} is not available for busy", agv.CorrelationId);
            return;
        }
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(PrepareAgvForStartingAction), agv.CorrelationId);
            return;
        }

        await PublishStateChange(context, agv);

        await context.Publish(new AgvPreparedForStartingAction(agvCorrelationId, action));
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

    /// <summary>
    /// Updates Cargos and publishes <see cref="CargoChanged" /> if there are changes
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<UpdateCargo> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        IReadOnlyCollection<Cargo> receivedCargos = context.Message.Cargos;

        var cargos = _mapper.Map<Collection<Domain.Models.Agv.Cargo>>(receivedCargos);
        if (await _cargoRepository.SetCargoAsync(agvCorrelationId, cargos))
        {
            await context.Publish(new CargoChanged(agvCorrelationId, receivedCargos));
        }
    }

    private async Task TryAutoAccept(IPublishEndpoint context, Domain.Models.Agv.Agv agv, CancellationToken cancellationToken)
    {
        _logger.LogDebug("AutoAccept {CorrelationId}", agv.CorrelationId);
        if (_productionOptions.AutoAccept && agv is {GraphExists: true, DataExist: true, CurrentAgvState: AgvState.Discovered})
        {
            _logger.LogDebug("AutoAccept {CorrelationId}, Graph: {GraphExists}, Data: {DataExists}", agv.CorrelationId, agv.GraphExists, agv.DataExist);
            await TransitionToIdleAsync(context, agv, cancellationToken);
        }
    }

    private async Task TransitionToIdleAsync(IPublishEndpoint context, Domain.Models.Agv.Agv agv, CancellationToken cancellationToken)
    {
        bool stateChanged = agv.TransitionToIdle();
        if (!await _agvRepository.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {CorrelationId}", nameof(HandleActionCompletion), agv.CorrelationId);
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
        }
    }

    private async Task PublishStateChange(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvStateChanged(agv.CorrelationId, agv.CurrentAgvState, agv.PreviousAgvState, agvMessageContent));
    }

    private async Task PublishGraphRequestAsync(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        _logger.LogDebug("Requesting graph for agv {CorrelationId}", agv.CorrelationId);
        await context.Publish(new RequestAgvGraph(agv.CorrelationId, agv.AgvId));
        agv.AgvGraphRequestedLastTime = DateTime.UtcNow;
    }

    private async Task PublishDataRequestAsync(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        _logger.LogDebug("Requesting data for agv {CorrelationId}", agv.CorrelationId);
        await context.Publish(new RequestAgvData(agv.CorrelationId, agv.AgvId));
        agv.AgvDataRequestedLastTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an agv.
    /// </summary>
    private async Task CreateAgv(ConsumeContext<UpdateAgvStatus> context)
    {
        (var correlationId, string agvId, var agvStatus) = context.Message;

        _logger.LogDebug("Creating agv {CorrelationId}, {AgvId}", correlationId, agvId);

        var agv = new Domain.Models.Agv.Agv(correlationId, agvId, _mapper.Map<AgvStatus>(agvStatus));
        bool graphExists = await _graphRepository.ExistsAsync(correlationId);
        if (!graphExists)
        {
            await PublishGraphRequestAsync(context, agv);
        }

        agv.TransitionToDiscovered(graphExists);
        agv.AgvDataRequestedLastTime = DateTime.UtcNow;
        await _agvRepository.AddAgvAsync(agv);
        await PublishDataRequestAsync(context, agv);

        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvDiscovered(agvMessageContent));
    }
}