using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs
{
    /// <summary>
    /// State Machine used to track AGVs
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvStateMachine : MassTransitStateMachine<AgvSaga>
    {
        private readonly ILogger<AgvStateMachine> _logger;

        private readonly IAgvDataRegistry _agvDataRegistry;
        private readonly IMapRegistry _mapRegistry;

        private readonly ProductionOptions _productionOptions;

        /// <summary>
        /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are still null
        /// </summary>
        /// <remarks>Interval indicated in seconds</remarks>
        public static int AgvRequestTimeout => 30;

        /// <summary>
        /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are already given
        /// </summary>
        /// <remarks>Interval indicated in seconds</remarks>
        private const int DailyRequestUpdateInterval = 24 * 3600;

        private const double BatteryChargeChangedThreshold = 0.01;

        private readonly ISettingsProvider<AgvSettings> _agvSettingsProvider;
        private readonly IAgvGraphRegistry _agvGraphRegistry;

        #region States
        /// <summary>
        /// New AGV discovered by receiving AgvStatusReceived while missing information evaluating the current state of the AGV.
        /// </summary>
        /// <remarks>Hypothetical, receiving <see cref="AgvStatusReceived"/> will lead to one of the subsequent states.</remarks>
        public State Discovered { get; private set; } = default!;

        /// <summary>
        /// Idle.
        /// </summary>
        public State Idle { get; private set; } = default!;

        /// <summary>
        /// Busy.
        /// </summary>
        public State Busy { get; private set; } = default!;

        /// <summary>
        /// Charging.
        /// </summary>
        public State Charging { get; private set; } = default!;
        #endregion

        #region Events
        /// <summary>
        /// Agv status has been received
        /// </summary>
        public Event<AgvStatusReceived> AgvStatusReceivedEvent { get; private set; } = default!;

        /// <summary>
        /// Request to reserve AGV 
        /// </summary>
        public Event<SetAgvBusy> SetAgvBusyEvent { get; private set; } = default!;

        /// <summary>
        /// Request to ask if Agv can be reserved 
        /// </summary>
        public Event<RequestCanAgvTransitionToBusy> RequestCanAgvTransitionToBusyEvent { get; private set; } = default!;

        /// <summary>
        /// Send AGV back to idle
        /// </summary>
        public Event<SetAgvIdle> SetAgvIdleEvent { get; private set; } = default!;

        /// <summary>
        /// AGV is ready to be assigned to orders
        /// </summary>
        public Event<AgvReady> AgvReadyEvent { get; private set; } = default!;

        /// <summary>
        /// Send AGV back to idle
        /// </summary>
        public Event<ForceReleaseAgv> ForceReleaseAgvEvent { get; private set; } = default!;

        /// <summary>
        /// Lock AGV to prevent it from being assigned to transport orders
        /// </summary>
        public Event<LockAgv> LockAgvEvent { get; private set; } = default!;

        /// <summary>
        /// Unlock AGV
        /// </summary>
        public Event<UnlockAgv> UnlockAgvEvent { get; private set; } = default!;

        /// <summary>
        /// The graph of the Agv has changed
        /// </summary>
        public Event<AgvGraphChanged> AgvGraphChangedEvent { get; private set; } = default!;

        /// <summary>
        /// Agv map is incoming
        /// </summary>
        public Event<AgvMapReceived> AgvMapReceivedEvent { get; private set; } = default!;

        /// <summary>
        /// Agv has started charging
        /// </summary>
        public Event<AgvStartedCharging> AgvStartedChargingEvent { get; private set; } = default!;

        /// <summary>
        /// Agv has stopped charging
        /// </summary>
        public Event<AgvStoppedCharging> AgvStoppedChargingEvent { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionStartedEvent"/> was received
        /// </summary>
        public Event<ActionStartedEvent> ActionStartedEvent { get; private set; } = default!;

        /// <summary>
        /// Agv error status was received
        /// </summary>
        public Event<AgvErrorStatusReceived> AgvErrorStatusReceivedEvent { get; private set; } = default!;
        #endregion

        /// <summary>
        /// Watchdog to detect AGV offline
        /// </summary>
        public Schedule<AgvSaga, AgvOfflineMessage> AgvOfflineWatchdog { get; private set; } = default!;

        private AgvSettings GetSettings(AgvSaga saga)
        {
            var settings = _agvSettingsProvider.GetSettings();

            return settings;
        }

        /// <summary>
        /// Constructor of AgvStateMachine
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="agvDataRegistry"></param>
        /// <param name="mapRegistry"></param>
        /// <param name="agvStateObserver"></param>
        /// <param name="productionOptions"></param>
        /// <param name="agvSettingsProvider"></param>
        /// <param name="agvGraphRegistry"></param>
        public AgvStateMachine(
            ILogger<AgvStateMachine> logger,
            IAgvDataRegistry agvDataRegistry,
            IMapRegistry mapRegistry,
            IStateObserver<AgvSaga> agvStateObserver,
            ProductionOptions productionOptions,
            ISettingsProvider<AgvSettings> agvSettingsProvider,
            IAgvGraphRegistry agvGraphRegistry)
        {
            _logger = logger;
            _agvDataRegistry = agvDataRegistry;
            _mapRegistry = mapRegistry;
            _productionOptions = productionOptions;
            _agvSettingsProvider = agvSettingsProvider;
            _agvGraphRegistry = agvGraphRegistry;

            SetSagaFactory();
            SetScheduler();
            InstanceState(x => x.CurrentState);
            Initially(InitialAgvStatusReceivedHandler());

            During(Idle, Charging, AcceptSetBusyRequestHandler());
            During(Discovered, Busy, RejectSetBusyRequestHandler());
            During(Discovered, Busy, AcceptSetIdleRequestHandler());
            During(Idle, Charging, RejectSetIdleRequestHandler());

            During(Idle, Busy, AgvStartedChargingReceivedHandler());
            During(Charging, AgvStoppedChargingReceivedHandler());

            DuringAny(SetAgvBusyAvailableEventHandler());
            DuringAny(LockAgvHandler());
            DuringAny(UnlockAgvHandler());
            DuringAny(ForceSetIdleHandler());
            DuringAny(AgvStatusReceivedHandler());
            DuringAny(AgvGraphChangedHandler());
            DuringAny(AgvMapReceivedHandler());
            DuringAny(AgvOfflineMessageHandler());
            DuringAny(AgvErrorStatusReceivedHandler());

            WhenEnterAny(UpdateTimeOfStateTransition);

            ConnectStateObserver(agvStateObserver);
        }

        #region Init
        /// <summary>
        /// Set saga factory on AgvStatusReceivedEvent
        /// </summary>
        private void SetSagaFactory()
        {
            // Initial creation
            Event(() => AgvStatusReceivedEvent, e =>
                e.SetSagaFactory(context => new AgvSaga(context.Message.CorrelationId, context.Message.AgvId, agvStatus: context.Message.AgvStatus))
            );
        }

        /// <summary>
        /// Initialize scheduled events
        /// </summary>
        private void SetScheduler()
        {
            Schedule(() => AgvOfflineWatchdog,
                instance => instance.AgvOfflineTokenId, configuration => { configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId); });
        }
        #endregion

        #region State transitions
        /// <summary>
        /// First message which creates a new agv saga instance
        /// </summary>
        /// <returns>Event Binding for AgvStatusReceived</returns>
        private EventActivityBinder<AgvSaga, AgvStatusReceived> InitialAgvStatusReceivedHandler() =>
            When(AgvStatusReceivedEvent)
                .TransitionTo(Discovered)
                .Then(c =>
                    _logger.LogInformation("New AGV {AgvId} / {CorrelationId} discovered", c.Message.AgvId, c.Saga.CorrelationId)
                )
                .ThenAsync(PublishRequestAgvDataAsync)
                .ThenAsync(async context =>
                {
                    if (!await _agvGraphRegistry.ExistsAsync(context.Saga.CorrelationId))
                    {
                        await PublishRequestAgvGraphAsync(context);
                    }
                });

        private EventActivityBinder<AgvSaga, RequestCanAgvTransitionToBusy> SetAgvBusyAvailableEventHandler() =>
            When(RequestCanAgvTransitionToBusyEvent)
                .RespondAsync(async c =>
                    new CanAgvTransitionToBusyResponse(
                        c.Message.CorrelationId,
                        await IsAgvCompleteAndUnlocked(c.Saga)
                        && (c.Saga.CurrentState is nameof(Idle) || (c.Saga.CurrentState is nameof(Charging) && c.Message.IgnoreCharging))
                    ));

        private EventActivityBinder<AgvSaga, SetAgvBusy> AcceptSetBusyRequestHandler() =>
            When(SetAgvBusyEvent)
                .IfElseAsync(async c => await IsAgvCompleteAndUnlocked(c.Saga), ifContext =>
                        ifContext.TransitionTo(Busy)
                            .Then(c => _logger.LogDebug("Transition AGV {AgvId} / {CorrelationId} to busy", c.Saga.AgvId, c.Saga.CorrelationId))
                            .Respond(c => new SetAgvBusySucceeded(c.Message.CorrelationId)),
                    elseContext =>
                        elseContext.Then(c => _logger.LogError("Could not transition AGV {AgvId} / {CorrelationId} to busy, AGV not Ready", c.Saga.AgvId, c.Saga.CorrelationId))
                            .Respond(c => new SetAgvBusyRejected(c.Message.CorrelationId)));

        private async Task<bool> IsAgvCompleteAndUnlocked(AgvSaga saga)
        {
            bool isReady = saga is
            {
                AgvStatus: not null,
                AgvStatus.LastNodeId: not null,
                IsLocked: false,
                HasError: false
            };

            if (!isReady)
            {
                return isReady;
            }

            // check graph last as this is expensive
            bool hasGraph = await _agvGraphRegistry.ExistsAsync(saga.CorrelationId);
            return hasGraph;
        }

        private EventActivityBinder<AgvSaga, SetAgvIdle> AcceptSetIdleRequestHandler() =>
            When(SetAgvIdleEvent)
                .TransitionTo(Idle)
                .Then(c =>
                    _logger.LogDebug("AGV {AgvId} / {CorrelationId} idle", c.Saga.AgvId, c.Saga.CorrelationId))
                .Respond(c => new SetAgvIdleSucceeded(c.Message.CorrelationId))
                .ThenAsync(c => PublishAgvBecameIdleAsync(c.Saga, c));

        private EventActivityBinder<AgvSaga, SetAgvBusy> RejectSetBusyRequestHandler() =>
            When(SetAgvBusyEvent)
                .Then(c => _logger.LogDebug("Could not transition {AgvId} / {CorrelationId} to busy in state {State}", c.Saga.AgvId, c.Saga.CorrelationId, c.Saga.CurrentState))
                .Respond(c => new SetAgvBusyRejected(c.Message.CorrelationId));

        private EventActivityBinder<AgvSaga, SetAgvIdle> RejectSetIdleRequestHandler() =>
            When(SetAgvIdleEvent)
                .Then(c => _logger.LogDebug("Could not transition {AgvId} / {CorrelationId} to idle in state {State}", c.Saga.AgvId, c.Saga.CorrelationId, c.Saga.CurrentState))
                .Respond(c => new SetAgvIdleRejected(c.Message.CorrelationId));

        private EventActivityBinder<AgvSaga, ForceReleaseAgv> ForceSetIdleHandler() =>
            When(ForceReleaseAgvEvent)
                .TransitionTo(Idle)
                .ThenAsync(c => PublishAgvBecameIdleAsync(c.Saga, c));

        private EventActivityBinder<AgvSaga, LockAgv> LockAgvHandler() =>
            When(LockAgvEvent)
                .ThenAsync(async c => { await LockAsync(c, true); })
                .Then(c =>
                    _logger.LogDebug("Agv {AgvId}/{CorrelationId} was locked", c.Saga.AgvId, c.Saga.CorrelationId));

        private EventActivityBinder<AgvSaga, UnlockAgv> UnlockAgvHandler() =>
            When(UnlockAgvEvent)
                .ThenAsync(async c => { await LockAsync(c, false); })
                .Then(c =>
                    _logger.LogDebug("Agv {AgvId}/{CorrelationId} was unlocked", c.Saga.AgvId, c.Saga.CorrelationId));

        private EventActivityBinder<AgvSaga, AgvStartedCharging> AgvStartedChargingReceivedHandler() =>
            When(AgvStartedChargingEvent)
                .TransitionTo(Charging)
                .Then(c => _logger.LogInformation("{AgvId} / {CorrelationId} has reached charging station (BatteryLevel: {BatteryLevel}%) => Transitioned to {State}",
                    c.Saga.AgvId, c.Saga.CorrelationId, c.Saga.AgvStatus!.BatteryCharge, nameof(Charging)));

        private EventActivityBinder<AgvSaga, AgvStoppedCharging> AgvStoppedChargingReceivedHandler() =>
            When(AgvStoppedChargingEvent)
                .TransitionTo(Idle)
                .Then(c => _logger.LogInformation("{AgvId} / {CorrelationId} reloaded its battery level to {BatteryLevel}% => Transitioned to {State}",
                    c.Saga.AgvId, c.Saga.CorrelationId, c.Saga.AgvStatus!.BatteryCharge, nameof(Idle)))
                .ThenAsync(c => PublishAgvBecameIdleAsync(c.Saga, c));
        #endregion

        #region Message Handlers
        /// <summary>
        /// Handle <see cref="AgvStatusReceived"/> message
        /// </summary>
        private EventActivityBinder<AgvSaga, AgvStatusReceived> AgvStatusReceivedHandler() =>
            When(AgvStatusReceivedEvent)
                .IfElse(cond => !cond.Saga.IsOnline,
                    t => t.Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId}, AGV became alive", nameof(AgvStatusReceived), c.Message.AgvId, c.Saga.CorrelationId)),
                    e => e.Then(c => _logger.LogTrace("{MessageType} {AgvId} / {CorrelationId}, AGV is alive", nameof(AgvStatusReceived), c.Message.AgvId, c.Saga.CorrelationId)))
                .ThenAsync(async c => await UpdateAgvStatusAsync(c.Saga, c.Message.AgvStatus, true, c))
                .ThenAsync(RequestAgvDataAndGraph)
                .ThenAsync(async context =>
                {
                    if (_productionOptions.AutoAccept &&
                        _agvDataRegistry.Exists(context.Saga.CorrelationId) &&
                        context.Saga.CurrentState == nameof(Discovered) &&
                        await _agvGraphRegistry.ExistsAsync(context.Saga.CorrelationId))
                    {
                        var graph = await _agvGraphRegistry.GetAgvGraphAsync(context.Saga.CorrelationId);

                        if (_mapRegistry.Exists(graph.RelatedMapId))
                        {
                            await context.Publish(new SetAgvIdle(context.Saga.CorrelationId));
                        }
                    }
                })
                .Schedule(AgvOfflineWatchdog,
                    c1 => new AgvOfflineMessage(c1.Saga.CorrelationId, DateTime.UtcNow),
                    c2 =>
                    {
                        var agvSettings = GetSettings(c2.Saga);
                        return TimeSpan.FromMilliseconds(c2.Saga.AgvStatus!.Busy ? agvSettings.AgvOfflineTimeoutMsWhileBusy : agvSettings.AgvOfflineTimeoutMsWhileIdle);
                    })
                .Then(c => _logger.LogTrace("{AgvId} watchdog started", c.Message.AgvId));

        private EventActivityBinder<AgvSaga, AgvGraphChanged> AgvGraphChangedHandler() =>
            When(AgvGraphChangedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} {CorrelationId}", nameof(AgvGraphChanged), c.Saga.CorrelationId))
                .ThenAsync(AgvGraphChangedAsync);

        private EventActivityBinder<AgvSaga, AgvMapReceived> AgvMapReceivedHandler() =>
            When(AgvMapReceivedEvent)
                .Then(c => _logger.LogInformation("Incoming Map {MapId}/{MapCorrelationId} for Agv {AgvId}/{CorrelationId}",
                    c.Message.AgvMap.MapId, c.Message.AgvMap.CorrelationId, c.Saga.AgvId, c.Saga.CorrelationId))
                .Then(SaveMap);

        private EventActivityBinder<AgvSaga, AgvOfflineMessage> AgvOfflineMessageHandler() =>
            When(AgvOfflineWatchdog.Received)
                .ThenAsync(async c => await UpdateAgvStatusAsync(c.Saga, c.Saga.AgvStatus, false, c))
                .Then(c => _logger.LogWarning("{MessageType} {AgvId} / {CorrelationId} received after timeout of {Timeout} ms, AGV is offline",
                    nameof(AgvOfflineMessage), c.Saga.AgvId, c.Saga.CorrelationId, (DateTime.UtcNow - c.Message.Created).TotalMilliseconds));

        /// <summary>
        /// Handle <see cref="AgvErrorStatusReceived"/> message
        /// </summary>
        private EventActivityBinder<AgvSaga, AgvErrorStatusReceived> AgvErrorStatusReceivedHandler() =>
            When(AgvErrorStatusReceivedEvent)
                .ThenAsync(async c => await UpdateAgvErrorStatusAsync(c.Saga, c.Message.Errors?.Any() ?? false, c));
        #endregion

        #region Actions
        private async Task RequestAgvDataAndGraph(BehaviorContext<AgvSaga, AgvStatusReceived> context)
        {
            var agvSaga = context.Saga;

            // When AgvData missing repeat request all x seconds otherwise refresh once a day
            int timeout = _agvDataRegistry.Exists(context.Saga.CorrelationId) ? DailyRequestUpdateInterval : AgvRequestTimeout;
            double timeSinceLastRequest = agvSaga.GetTimeSpanSinceLastRequest_AgvData().TotalSeconds;
            if (timeSinceLastRequest > timeout)
            {
                await context.Publish(new RequestAgvData(agvSaga.CorrelationId, agvSaga.AgvId));
                agvSaga.AgvDataRequestedLastTime = DateTime.UtcNow;
            }

            // When AgvGraph is missing repeat request all x seconds otherwise refresh once a day
            timeout = DailyRequestUpdateInterval;
            timeSinceLastRequest = agvSaga.GetTimeSpanSinceLastRequest_AgvGraph().TotalSeconds;
            if (timeSinceLastRequest > timeout)
            {
                await PublishRequestAgvGraphAsync(context);
            }
        }

        /// <summary>
        /// When publishing <see cref="RequestAgvData"/> automatically store <see cref="AgvSaga.AgvDataRequestedLastTime"/>.
        /// </summary>
        private static async Task PublishRequestAgvDataAsync(BehaviorContext<AgvSaga, AgvStatusReceived> context)
        {
            await context.Publish(new RequestAgvData(context.Saga.CorrelationId, context.Saga.AgvId));
            context.Saga.AgvDataRequestedLastTime = DateTime.UtcNow;
        }

        /// <summary>
        /// When publishing <see cref="RequestAgvGraph"/> automatically store <see cref="AgvSaga.AgvGraphRequestedLastTime"/>.
        /// </summary>
        private static async Task PublishRequestAgvGraphAsync(BehaviorContext<AgvSaga, AgvStatusReceived> context)
        {
            await context.Publish(new RequestAgvGraph(context.Saga.CorrelationId, context.Saga.AgvId));
            context.Saga.AgvGraphRequestedLastTime = DateTime.UtcNow;
        }

        private async Task PublishAgvBecameIdleAsync(AgvSaga agvSaga, IPublishEndpoint endpoint)
        {
            await endpoint.Publish(new AgvBecameIdle(agvSaga.CorrelationId));
            _logger.LogDebug("Agv {AgvId}/{CorrelationId} was released and transitioned to Idle", agvSaga.AgvId, agvSaga.CorrelationId);
        }

        private async Task UpdateAgvStatusAsync(AgvSaga agvSaga, AgvStatus? newStatus, bool newAlive, IPublishEndpoint endpoint)
        {
            if (agvSaga.AgvStatus != null && newStatus != null)
            {
                double oldCharge = agvSaga.AgvStatus.BatteryCharge;
                double newCharge = newStatus.BatteryCharge;

                if (Math.Abs(oldCharge - newCharge) > BatteryChargeChangedThreshold)
                {
                    await endpoint.Publish(new AgvBatteryChargeChanged(agvSaga.CorrelationId, oldCharge, newCharge));
                }

                string? oldLastNodeId = agvSaga.AgvStatus.LastNodeId;
                string? newLastNodeId = newStatus.LastNodeId;

                if (oldLastNodeId != newLastNodeId)
                {
                    await endpoint.Publish(new AgvLastNodeIdChanged(agvSaga.CorrelationId, oldLastNodeId, newLastNodeId));
                }
            }

            bool aliveChanged = agvSaga.IsOnline != newAlive; // AGV alive has changed
            bool stateChanged = !Equals(agvSaga.AgvStatus, newStatus); // Equals() checks value equality for record types

            agvSaga.IsOnline = newAlive;
            agvSaga.AgvStatus = newStatus;

            if (aliveChanged || stateChanged) // Publish AgvPropertyChanged only something has actually changed
            {
                await endpoint.Publish(new AgvPropertyChanged(agvSaga, agvSaga.CorrelationId));
            }

            if (aliveChanged && newAlive && agvSaga.CurrentState == nameof(Idle)) // Transition to Ready must happen before triggering order assignment
            {
                await endpoint.Publish(new AgvBecameIdle(agvSaga.CorrelationId));
            }
        }

        private static async Task LockAsync(BehaviorContext<AgvSaga, object> c, bool isLocked)
        {
            var agvSaga = c.Saga;
            bool changed = agvSaga.IsLocked != isLocked;
            agvSaga.IsLocked = isLocked;
            agvSaga.TimeOfLockStateChanged = DateTime.UtcNow;

            if (changed)
            {
                await c.Publish(new AgvPropertyChanged(agvSaga, agvSaga.CorrelationId));
            }
        }

        private static async Task UpdateAgvErrorStatusAsync(AgvSaga agvSaga, bool isInError, IPublishEndpoint endpoint)
        {
            bool agvIsInErrorChanged = agvSaga.HasError != isInError;
            agvSaga.HasError = isInError;

            if (agvIsInErrorChanged)
            {
                await endpoint.Publish(new AgvPropertyChanged(agvSaga, agvSaga.CorrelationId));
            }
        }

        private async Task AgvGraphChangedAsync(BehaviorContext<AgvSaga, AgvGraphChanged> context)
        {
            var agvGraph = context.Message.AgvGraph;

            if (!_mapRegistry.Exists(agvGraph.RelatedMapId))
            {
                await context.Publish(new RequestAgvMap(context.Saga.CorrelationId, context.Saga.AgvId, agvGraph.RelatedMapId, agvGraph.RelatedMapName));
            }
        }

        private void SaveMap(BehaviorContext<AgvSaga, AgvMapReceived> context)
        {
            var agvMap = context.Message.AgvMap;

            if (!_mapRegistry.Exists(agvMap.CorrelationId))
            {
                if (_mapRegistry.AddOrUpdate(agvMap.CorrelationId, agvMap.MapId, agvMap.ImageData, agvMap.Resolution, agvMap.XOffset, agvMap.YOffset, agvMap.ModificationDate))
                {
                    _logger.LogInformation("Map {MapId}/{MapCorrelationId} inserted into Registry", agvMap.MapId, agvMap.CorrelationId);
                }
                else
                {
                    _logger.LogWarning("Failed to insert Map {MapId}/{MapCorrelationId} into Registry", agvMap.MapId, agvMap.CorrelationId);
                }
            }
            else
            {
                if (_mapRegistry.AddOrUpdate(agvMap.CorrelationId, agvMap.MapId, agvMap.ImageData, agvMap.Resolution, agvMap.XOffset, agvMap.YOffset, agvMap.ModificationDate))
                {
                    _logger.LogInformation("Map {MapId}/{MapCorrelationId} has been updated in Registry", agvMap.MapId, agvMap.CorrelationId);
                }
                else
                {
                    _logger.LogWarning("Failed to update Map {MapId}/{MapCorrelationId} in Registry", agvMap.MapId, agvMap.CorrelationId);
                }
            }
        }

        private EventActivityBinder<AgvSaga> UpdateTimeOfStateTransition(EventActivityBinder<AgvSaga> eventActivity) =>
            eventActivity.Then(ctx => ctx.Saga.TimeOfStateTransition = DateTime.UtcNow);
        #endregion
    }
}