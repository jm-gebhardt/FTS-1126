using System.Collections.ObjectModel;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvSettings = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings.AgvSettings;
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
    IConsumer<AbortChargeAction>,
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
    IConsumer<UpdateCargo>,
    IConsumer<ResetOfflineWatchdog>
{
    private readonly ILogger<AgvCommandHandler> _logger;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _agvsSemaphore;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;
    private readonly IMapper _mapper;
    private readonly ProductionOptions _productionOptions;
    private readonly IRequestClient<QueueActionGroup> _queueActionGroupRequestClient;
    private readonly IAgvDb _agvDb;

    /// <inheritdoc cref="AgvCommandHandler"/>
    public AgvCommandHandler(ISemaphoreProvider<IAgvsSemaphore> agvsSemaphore,
        IAgvDb agvDb,
        ISettingsProvider<AgvSettings> settingsProvider,
        IActionGroupRepository actionGroupRepository,
        IRequestClient<QueueActionGroup> queueActionGroupRequestClient,
        IMapper mapper,
        ProductionOptions productionOptions,
        ILogger<AgvCommandHandler> logger
    )
    {
        _logger = logger;
        _agvsSemaphore = agvsSemaphore;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _settingsProvider = settingsProvider;
        _mapper = mapper;
        _queueActionGroupRequestClient = queueActionGroupRequestClient;
        _productionOptions = productionOptions;
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
            ICollection<Domain.Models.Agv.Agv> goodAgvs = await _agvDb.Agvs.GetChargingAgvsWithMinBatteryLevelAsync(agvSettings.MinChargeUp);

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
        (var correlationId, List<AgvError> errors) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        bool agvHasError = errors.Any();

        foreach (var agvError in errors.Where(e => e.ErrorLevel == ErrorLevel.Fatal))
        {
            _logger.LogAgvError(306, agv.CorrelationId, "AGV Error", $"AGV {agv.SerialNumber} threw error {agvError.ErrorDescription}");
        }
        foreach (var agvError in errors.Where(e => e.ErrorLevel == ErrorLevel.Warning))
        {
            _logger.LogAgvWarning(307, agv.CorrelationId, "AGV Warning", $"AGV {agv.SerialNumber} threw warning {agvError.ErrorDescription}");
        }

        if (agvHasError != agv.HasError)
        {
            agv.HasError = agvHasError;
            await _agvDb.Agvs.UpdateAgvAsync(agv);
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
        Domain.Models.Agv.Agv agv;
        try
        {
            agv = await _agvDb.Agvs.GetAgvAsync(correlationId);
        }
        catch (AgvNotFoundException)
        {
            await CreateAgv(context);
            return;
        }
        _logger.LogDebug("Updating AgvStatus for agv {AgvCorrelationId}, {OldStatus} -> {NewStatus}", correlationId, agv.AgvStatus?.ToString(), agvStatus.ToString());
        var oldStatus = agv.AgvStatus;
        bool wasOnline = agv.IsOnline;

        await ResetOfflineWatchDog(context, agv);
        bool wasReady = agv.IsReady;
        (bool batteryChargeChanged, bool lastNodeIdChanged) = agv.UpdateAgvStatus(agvStatus);
        bool isReady = agv.IsReady;

        LogBatteryStates(oldStatus, agv.AgvStatus);
        PublishMetrics(agv.AgvStatus);

        if (!wasReady && isReady)
        {
            _logger.LogAgvInformation(303, correlationId, "AGV Ready", $"AGV {agv.SerialNumber} became ready");
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
        var agvStatus = agv.AgvStatus!;
        if (batteryChargeChanged)
        {
            var agvSettings = _settingsProvider.GetSettings();

            await context.Publish(new AgvBatteryChargeChanged(agv.CorrelationId,
                oldStatus!.BatteryCharge,
                agvStatus.BatteryCharge,
                agvSettings,
                agvStatus,
                // some time estimation
                agvStatus.Charging ? DateTime.UtcNow + TimeSpan.FromSeconds((100 - agvStatus.BatteryCharge) * 1) : null,
                DateTime.UtcNow.AddDays(-2)));
        }
        if (lastNodeIdChanged)
        {
            await context.Publish(new AgvLastNodeIdChanged(agv.CorrelationId, oldStatus!.LastNodeId, agvStatus.LastNodeId));
        }
        if (agv.CurrentAgvState == AgvState.Idle && !agvStatus.Busy && oldStatus!.Busy)
        {
            await context.Publish(new AgvBecameIdle(agv.CorrelationId));
        }
        if (!wasOnline && agv.IsOnline)
        {
            await context.Publish(new AgvOnlineChanged(agv.CorrelationId, true));
            _logger.LogAgvInformation(304, agvStatus.CorrelationId, "AGV Online", $"AGV {agv.SerialNumber} is online");
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

        _logger.LogAgvInformation(302, correlationId, "AGV Graph Received", $"AGV {agv.SerialNumber} graph has been received");
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
            _logger.LogAgvInformation(305, agv.CorrelationId, "AGV Offline", $"AGV {agv.SerialNumber} is offline");
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
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(UpdateAgvData), agv.CorrelationId);
        }
        _logger.LogAgvInformation(301, correlationId, "AGV Information Received", $"AGV {agv.SerialNumber} information has been received");
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
        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(LockAgv), agv.CorrelationId);
        }

        _logger.LogAgvInformation(308, agv.CorrelationId, "AGV Locked", $"AGV {agv.SerialNumber} is locked");
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

        if (!await _agvDb.Agvs.UpdateAgvAsync(agv))
        {
            _logger.LogDebug("{Command}: Could not update agv {AgvCorrelationId}", nameof(UnlockAgv), agv.CorrelationId);
        }
        _logger.LogAgvInformation(309, agv.CorrelationId, "AGV Unlocked", $"AGV {agv.SerialNumber} is unlocked");

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

        if (!await _agvDb.Graphs.NodeExistsAsync(targetNodeId))
        {
            _logger.LogError("Manual move of {AgvCorrelationId} failed, node {TargetNode} does not exist", agv.CorrelationId, targetNodeId);
            await context.RespondAsync(new AgvMoveFailed($"Node {targetNodeId} does not exist."));
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

        await _queueActionGroupRequestClient.GetResponse<Success>(new QueueActionGroup(moveActionGroup, true));
        await context.RespondAsync(new AgvMoveSuccessful(moveActionGroup.CorrelationId));
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
        bool isAvailable = agv.TryTransitionToBusy(true);

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
        }
        bool hasCargo = await _agvDb.Cargos.HasAgvCargoAsync(agvCorrelationId);
        if (hadCargo != hasCargo)
        {
            await context.Publish(new HasCargoChanged(agvCorrelationId, hasCargo));
        }
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        _logger.LogAgvInformation(310, agvCorrelationId, "Cargo Updated", $"AGV {agv.SerialNumber} cargo updated");
    }

    /// <summary>
    /// Handles the <see cref="ResetOfflineWatchdog"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetOfflineWatchdog> context)
    {
        var agv = await _agvDb.Agvs.GetAgvAsync(context.Message.CorrelationId);
        await ResetOfflineWatchDog(context, agv);
        await _agvDb.Agvs.UpdateAgvAsync(agv);
    }

    private async Task<Domain.Models.Agv.Agv?> TryGetAgv(Guid agvCorrelationId)
    {
        Domain.Models.Agv.Agv? agv = null;
        try
        {
            agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        }
        catch (AgvNotFoundException)
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
            _logger.LogAgvInformation(311, agv.CorrelationId, "AGV Became Idle", $"AGV {agv.SerialNumber} became idle");
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

        _logger.LogAgvInformation(300, correlationId, "AGV Discovered", $"AGV {agvSerialNumber} has been discovered");

        var agv = new Domain.Models.Agv.Agv(correlationId, agvSerialNumber, _mapper.Map<AgvStatus>(agvStatus));

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

    private void LogBatteryStates(AgvStatus? oldAgvStatus, AgvStatus? newAgvStatus)
    {
        if (newAgvStatus == null || oldAgvStatus == null)
        {
            return;
        }
        var agvSettings = _settingsProvider.GetSettings();
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
            _logger.LogBatteryInformation(205, oldAgvStatus.CorrelationId, "Battery Fully Charged", $"Battery has finished charging at {Math.Round(newAgvStatus.BatteryCharge)}%");
        }

        if (!wasMaxChargedUp && isMaxChargedUp)
        {
            _logger.LogBatteryInformation(204, oldAgvStatus.CorrelationId, "Maximum Charging Level", $"Battery has reached maximum charging level {agvSettings.MaxChargeUp}%");
        }

        if (!wasMinChargedUp && isMinChargedUp)
        {
            _logger.LogBatteryInformation(203, oldAgvStatus.CorrelationId, "Minimum Charging Level", $"Battery has reached minimum charging level {agvSettings.MinChargeUp}%");
        }

        if (!wasCritical && isCritical)
        {
            _logger.LogBatteryInformation(200, oldAgvStatus.CorrelationId, "Critical Charging Level", $"Battery has reached critical charging level {agvSettings.BatteryLevelCritical}%");
        }

        if (!wasLow && isLow)
        {
            _logger.LogBatteryInformation(203, oldAgvStatus.CorrelationId, "Low Charging Level", $"Battery has reached low charging level {agvSettings.BatteryLevelLow}%");
        }

        if (isCharging && !wasCharging)
        {
            _logger.LogBatteryInformation(201, oldAgvStatus.CorrelationId, "Started Charging", $"Battery started charging at level {Math.Round(newAgvStatus.BatteryCharge)}%");
        }
    }

    private void PublishMetrics(AgvStatus? agvStatus)
    {
        if (agvStatus == null)
        {
            return;
        }
        MetricCollector.BatteryChargeGauge.WithLabels(agvStatus.CorrelationId.ToString()).Set(agvStatus.BatteryCharge);
    }
}