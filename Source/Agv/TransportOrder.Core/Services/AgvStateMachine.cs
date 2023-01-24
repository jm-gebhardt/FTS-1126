using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// State Machine used to track AGVs
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvStateMachine : MassTransitStateMachine<AgvSaga>
    {
        /// <summary>
        /// Provide possibility to override default settings beside command line options
        /// <para>
        /// Useful for testing
        /// </para></summary>
        public interface ISettings
        {
            /// <summary>Overwrite <see cref="AgvStateMachine._agvOfflineTimeoutMsWhileIdle"/> (keep default when 0).</summary>
            public int AgvOfflineTimeoutMsWhileIdle { get; }

            /// <summary>Overwrite <see cref="AgvStateMachine._agvOfflineTimeoutMsWhileBusy"/> (keep default when 0).</summary>
            public int AgvOfflineTimeoutMsWhileBusy { get; }
        }

        private readonly ILogger<AgvStateMachine> _logger;

        private readonly IMapRegistry _mapRegistry;

        private readonly ProductionOptions _productionOptions;

        /// <summary>
        /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are still null
        /// </summary>
        /// <remarks>Interval indicated in seconds</remarks>
        public int AgvRequestTimeoutS => 30;

        /// <summary>
        /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are already given
        /// </summary>
        /// <remarks>Interval indicated in seconds</remarks>
        private const int DailyRequestUpdateInterval = 24 * 3600;

        /// <summary>
        /// Timeout value [ms] to set AGV offline while it is in idle mode
        /// </summary>
        private readonly int _agvOfflineTimeoutMsWhileIdle = 15000;
        /// <summary>
        /// Timeout value [ms] to set AGV offline while it is in busy mode
        /// </summary>
        private readonly int _agvOfflineTimeoutMsWhileBusy = 20000;

        #region Message handlers
        /// <summary>
        /// Internal message send once during startup to initialize AGV and start AGV supervision.
        /// </summary>
        /// <param name="CorrelationId"></param>
        /// <param name="AgvId"></param>
        public record AgvInitializeMessage(Guid CorrelationId, string AgvId) : CorrelatedBy<Guid>;
        #endregion

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
        /// Reserved.
        /// </summary>
        public State Reserved { get; private set; } = default!;

        /// <summary>
        /// Busy.
        /// </summary>
        public State Busy { get; private set; } = default!;


        /// <summary>
        /// PendingLock.
        /// </summary>
        public State PendingLock { get; private set; } = default!;

        /// <summary>
        /// Locked.
        /// </summary>
        public State Locked { get; private set; } = default!;

        /// <summary>
        /// Charging.
        /// </summary>
        public State Charging { get; private set; } = default!;

        #endregion

        #region Events
        /// <summary>
        /// Request for AGV initialization
        /// </summary>
        public Event<AgvInitializeMessage> AgvInitializeMessageEvent { get; private set; } = default!;

        /// <summary>
        /// Agv status has been received
        /// </summary>
        public Event<AgvStatusReceived> AgvStatusReceivedEvent { get; private set; } = default!;

        /// <summary>
        /// Request to reserve AGV 
        /// </summary>
        public Event<ReserveAgv> ReserveAgvEvent { get; private set; } = default!;

        /// <summary>
        /// Send AGV back to idle
        /// </summary>
        public Event<ReleaseAgv> ReleaseAgvEvent { get; private set; } = default!;

        /// <summary>
        /// Send AGV back to idle
        /// </summary>
        public Event<ForceReleaseAgv> ForceReleaseAgvEvent { get; private set; } = default!;

        /// <summary>
        /// Lock AGV to prevent it from being assigned to transport orders
        /// </summary>
        public Event<LockAgv> LockAgvEvent { get; private set; } = default!;

        /// <summary>
        /// The graph of the Agv was received
        /// </summary>
        public Event<AgvGraphReceived> AgvGraphReceivedEvent { get; private set; } = default!;

        /// <summary>
        /// The data of the Agv was received
        /// </summary>
        public Event<AgvDataReceived> AgvDataReceivedEvent { get; private set; } = default!;

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
        #endregion

        /// <summary>
        /// Watchdog to detect AGV offline
        /// </summary>
        public Schedule<AgvSaga, AgvOfflineMessage> AgvOfflineWatchdog { get; private set; } = default!;

        /// <summary>
        /// Constructor of AgvStateMachine
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="mapRegistry"></param>
        /// <param name="agvStateObserver"></param>
        /// <param name="productionOptions"></param>
        /// <param name="settings"></param>
        public AgvStateMachine(ILogger<AgvStateMachine> logger, IMapRegistry mapRegistry, IStateObserver<AgvSaga> agvStateObserver, ProductionOptions productionOptions, ISettings? settings = null)
        {
            _logger = logger;
            _mapRegistry = mapRegistry;
            _productionOptions = productionOptions;

            if (settings != null)
            {
                if (settings.AgvOfflineTimeoutMsWhileBusy > 0)
                {
                    _agvOfflineTimeoutMsWhileBusy = settings.AgvOfflineTimeoutMsWhileBusy;
                }
                if (settings.AgvOfflineTimeoutMsWhileIdle > 0)
                {
                    _agvOfflineTimeoutMsWhileIdle = settings.AgvOfflineTimeoutMsWhileIdle;
                }
            }

            SetSagaFactory();
            SetScheduler();
            InstanceState(x => x.CurrentState);
            Initially(InitialAgvStatusReceivedHandler());

            During(Discovered, Reserved, Busy, ReleaseAgvHandler());
            During(PendingLock, ReleasePendingLockAgvHandler());
            During(Idle, AcceptReserveAgvRequestHandler());
            During(Discovered, Reserved, Busy, RejectReserveAgvRequestHandler());
            During(PendingLock, Locked, RejectReserveAgvRequestHandler());
            During(Discovered, Locked, ForceReleaseAgvHandler());
            During(Idle, Discovered, LockAgvHandler());
            During(Reserved, Busy, PendingLockAgvHandler());
            During(Reserved, Idle, AgvStartedChargingReceivedHandler());
            During(Charging, AgvStoppedChargingReceivedHandler());

            DuringAny(AgvInitializeMessageHandler());
            DuringAny(AgvStatusReceivedHandler());
            DuringAny(AgvGraphReceivedHandler());
            DuringAny(AgvDataReceivedHandler());
            DuringAny(AgvMapReceivedHandler());
            DuringAny(AgvOfflineMessageHandler());

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
                instance => instance.AgvOfflineTokenId, configuration =>
                {
                    configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
                });
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
                    _logger.LogInformation("New AGV {AgvId} / {CorrelationId} discovered", c.Message.AgvId, c.Saga.CorrelationId))
                .If(cond => cond.Saga.AgvData == null,
                    PublishRequestAgvData)
                .If(cond => cond.Saga.AgvGraph == null,
                    PublishRequestAgvGraph);

        private EventActivityBinder<AgvSaga, ReleaseAgv> ReleaseAgvHandler() =>
           When(ReleaseAgvEvent)
                .TransitionTo(Idle)
                .ThenAsync(c => PublishAgvBecameIdle(c.Saga, c));

        private EventActivityBinder<AgvSaga, ReleaseAgv> ReleasePendingLockAgvHandler() =>
            When(ReleaseAgvEvent)
                .TransitionTo(Locked)
                .ThenAsync(c => PublishAgvBecameIdle(c.Saga, c));

        private EventActivityBinder<AgvSaga, ReserveAgv> AcceptReserveAgvRequestHandler() =>
            When(ReserveAgvEvent)
                .TransitionTo(Reserved)
                .Then(c =>
                    _logger.LogDebug("AGV {AgvId} / {CorrelationId} reserved", c.Saga.AgvId, c.Saga.CorrelationId))
                .Respond(c => new AgvReservationSucceeded(c.Message.CorrelationId));

        private EventActivityBinder<AgvSaga, ReserveAgv> RejectReserveAgvRequestHandler() =>
            When(ReserveAgvEvent)
                .Then(c => _logger.LogDebug("Could not reserve {AgvId} / {CorrelationId} in state {State}", c.Saga.AgvId, c.Saga.CorrelationId, c.Saga.CurrentState))
                .Respond(c => new AgvReservationRejected(c.Message.CorrelationId));

        private EventActivityBinder<AgvSaga, ForceReleaseAgv> ForceReleaseAgvHandler() =>
            When(ForceReleaseAgvEvent)
                .TransitionTo(Idle)
                .ThenAsync(c => PublishAgvBecameIdle(c.Saga, c));

        private EventActivityBinder<AgvSaga, LockAgv> LockAgvHandler() =>
            When(LockAgvEvent)
                .TransitionTo(Locked)
                .Then(c =>
                    _logger.LogDebug("Agv {AgvId}/{CorrelationId} was locked", c.Saga.AgvId, c.Saga.CorrelationId));

        private EventActivityBinder<AgvSaga, LockAgv> PendingLockAgvHandler() =>
            When(LockAgvEvent)
                .TransitionTo(PendingLock)
                .Then(c =>
                    _logger.LogDebug("Agv {AgvId}/{CorrelationId} is pending to be locked", c.Saga.AgvId, c.Saga.CorrelationId));
        #endregion

        #region Message Handlers
        /// <summary>
        /// Handle <see cref="AgvInitializeMessage"/> message
        /// </summary>
        private EventActivityBinder<AgvSaga, AgvInitializeMessage> AgvInitializeMessageHandler() =>
            When(AgvInitializeMessageEvent)
                .Schedule(AgvOfflineWatchdog,
                          c => new AgvOfflineMessage(c.Saga.CorrelationId, DateTime.UtcNow),
                          _ => TimeSpan.FromMilliseconds(_agvOfflineTimeoutMsWhileBusy))
                .Then(c => _logger.LogTrace("{AgvId} watchdog started", c.Message.AgvId));

        /// <summary>
        /// Handle <see cref="AgvStatusReceived"/> message
        /// </summary>
        private EventActivityBinder<AgvSaga, AgvStatusReceived> AgvStatusReceivedHandler() =>
            When(AgvStatusReceivedEvent)
                .Then(c =>
                    _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId}, AGV is alive", nameof(AgvStatusReceived), c.Message.AgvId, c.Saga.CorrelationId))
                .If(cond => !cond.Saga.Alive, x => x
                    .Then(c => c.Saga.Alive = true)
                    .Publish(m => new AgvPropertyChanged(m.Saga, m.Saga.CorrelationId)))
                .Then(c => c.Saga.AgvStatus = c.Message.AgvStatus)
                // When AgvData missing repeat request all x seconds
                .If(cond => cond.Saga.AgvData == null && cond.Saga.GetTimeSpanSinceLastRequest_AgvData().TotalSeconds > AgvRequestTimeoutS,
                    PublishRequestAgvData)
                // When AgvData present refresh once a day
                .If(cond => cond.Saga.AgvData != null && cond.Saga.GetTimeSpanSinceLastRequest_AgvData().TotalSeconds > DailyRequestUpdateInterval,
                    PublishRequestAgvData)
                // When AgvGraph missing repeat request all x seconds
                .If(cond => cond.Saga.AgvGraph == null && cond.Saga.GetTimeSpanSinceLastRequest_AgvGraph().TotalSeconds > AgvRequestTimeoutS,
                    PublishRequestAgvGraph)
                // When AgvGraph present refresh once a day
                .If(cond => cond.Saga.AgvGraph != null && cond.Saga.GetTimeSpanSinceLastRequest_AgvGraph().TotalSeconds > DailyRequestUpdateInterval,
                    PublishRequestAgvGraph)
                .If(cond =>
                    _productionOptions.AutoAccept &&
                    cond.Saga.AgvData is not null &&
                    cond.Saga.AgvGraph is not null &&
                    _mapRegistry.Exists(cond.Saga.AgvGraph.RelatedMapId) &&
                    cond.Saga.CurrentState == nameof(Discovered),
                    m => m.Publish(c => new ReleaseAgv(c.Saga.CorrelationId)))
                .Schedule(AgvOfflineWatchdog,
                          c1 => new AgvOfflineMessage(c1.Saga.CorrelationId, DateTime.UtcNow),
                          c2 => TimeSpan.FromMilliseconds(c2.Saga.AgvStatus!.Busy ? _agvOfflineTimeoutMsWhileBusy : _agvOfflineTimeoutMsWhileIdle))
                .Then(c => _logger.LogTrace("{AgvId} watchdog started", c.Message.AgvId));

        private EventActivityBinder<AgvSaga, AgvGraphReceived> AgvGraphReceivedHandler() =>
            When(AgvGraphReceivedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId}", nameof(AgvGraphReceived), c.Message.AgvId, c.Saga.CorrelationId))
                .Then(c => c.Saga.AgvGraph = c.Message.Graph)
                .Then(SearchMapNameInRegistry);

        private EventActivityBinder<AgvSaga, AgvDataReceived> AgvDataReceivedHandler() =>
            When(AgvDataReceivedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId}", nameof(AgvDataReceived), c.Message.AgvId, c.Saga.CorrelationId))
                .Then(c => c.Saga.AgvData = c.Message.Data);

        private EventActivityBinder<AgvSaga, AgvMapReceived> AgvMapReceivedHandler() =>
            When(AgvMapReceivedEvent)
                .Then(c => _logger.LogInformation("Incoming Map {MapId}/{MapCorrelationId} for Agv {AgvId}/{CorrelationId}",
                    c.Message.AgvMap.MapId, c.Message.AgvMap.CorrelationId, c.Saga.AgvId, c.Saga.CorrelationId))
                .Then(SaveMap);

        private EventActivityBinder<AgvSaga, AgvOfflineMessage> AgvOfflineMessageHandler() =>
            When(AgvOfflineWatchdog.Received)
                .If(cond => cond.Saga.Alive, x => x
                    .Then(c => c.Saga.Alive = false)
                    .Publish(m => new AgvPropertyChanged(m.Saga, m.Saga.CorrelationId)))
                .Then(c => _logger.LogWarning("{MessageType} {AgvId} / {CorrelationId} received after timeout of {Timeout} ms, AGV is offline",
                    nameof(AgvOfflineMessage), c.Saga.AgvId, c.Saga.CorrelationId, (DateTime.UtcNow - c.Message.Created).TotalMilliseconds));
        #endregion

        #region Actions
        /// <summary>
        /// When publishing <see cref="RequestAgvData"/> automatically store <see cref="AgvSaga.AgvDataRequestedLastTime"/>.
        /// </summary>
        private EventActivityBinder<AgvSaga, AgvStatusReceived> PublishRequestAgvData(EventActivityBinder<AgvSaga, AgvStatusReceived> binder) =>
            binder.Publish(m => new RequestAgvData(m.Saga.CorrelationId, m.Saga.AgvId))
                  .Then(c => c.Saga.AgvDataRequestedLastTime = DateTime.UtcNow);

        /// <summary>
        /// When publishing <see cref="RequestAgvGraph"/> automatically store <see cref="AgvSaga.AgvGraphRequestedLastTime"/>.
        /// </summary>
        private EventActivityBinder<AgvSaga, AgvStatusReceived> PublishRequestAgvGraph(EventActivityBinder<AgvSaga, AgvStatusReceived> binder) =>
            binder.Publish(m => new RequestAgvGraph(m.Saga.CorrelationId, m.Saga.AgvId))
                  .Then(c => c.Saga.AgvGraphRequestedLastTime = DateTime.UtcNow);

        private async Task PublishAgvBecameIdle(AgvSaga agvSaga, IPublishEndpoint endpoint)
        {
            await endpoint.Publish(new AgvBecameIdle(agvSaga.CorrelationId));
            _logger.LogDebug("Agv {AgvId}/{CorrelationId} was released and transitioned to Idle", agvSaga.AgvId, agvSaga.CorrelationId);
        }

        private void SearchMapNameInRegistry(BehaviorContext<AgvSaga, AgvGraphReceived> context)
        {
            if (_mapRegistry.Exists(context.Message.Graph.RelatedMapId))
            {
                _logger.LogInformation("Map {MapName}/{MapCorrelationId} is already given in MapRegistry", context.Message.Graph.RelatedMapName, context.Message.Graph.RelatedMapId);
            }
            else
            {
                context.Publish(new RequestAgvMap(context.Saga.CorrelationId, context.Saga.AgvId, context.Message.Graph.RelatedMapId, context.Message.Graph.RelatedMapName));
            }
        }

        private void SaveMap(BehaviorContext<AgvSaga, AgvMapReceived> context)
        {
            if (!_mapRegistry.Exists(context.Message.AgvMap.CorrelationId))
            {
                if (_mapRegistry.AddOrUpdate(context.Message.AgvMap.CorrelationId, context.Message.AgvMap.MapId, context.Message.AgvMap.ImageData, context.Message.AgvMap.Resolution, context.Message.AgvMap.XOffset, context.Message.AgvMap.YOffset, context.Message.AgvMap.ModificationDate))
                {
                    _logger.LogInformation("Map {MapId}/{MapCorrelationId} inserted into Registry", context.Message.AgvMap.MapId, context.Message.CorrelationId);
                }
                else
                {
                    _logger.LogWarning("Failed to insert Map {MapId}/{MapCorrelationId} into Registry", context.Message.AgvMap.MapId, context.Message.CorrelationId);
                }
            }
            else
            {
                if (_mapRegistry.AddOrUpdate(context.Message.AgvMap.CorrelationId, context.Message.AgvMap.MapId, context.Message.AgvMap.ImageData, context.Message.AgvMap.Resolution, context.Message.AgvMap.XOffset, context.Message.AgvMap.YOffset, context.Message.AgvMap.ModificationDate))
                {
                    _logger.LogInformation("Map {MapId}/{MapCorrelationId} has been updated in Registry", context.Message.AgvMap.MapId, context.Message.CorrelationId);
                }
                else
                {
                    _logger.LogWarning("Failed to update Map {MapId}/{MapCorrelationId} in Registry", context.Message.AgvMap.MapId, context.Message.CorrelationId);
                }
            }
        }

        private EventActivityBinder<AgvSaga, AgvStartedCharging> AgvStartedChargingReceivedHandler() =>
            When(AgvStartedChargingEvent)
                .TransitionTo(Charging)
                .Then(c => _logger.LogInformation("{AgvId} / {CorrelationId} has reached charging station (BatteryLevel: {batteryLevel}%) => Transitioned to {State}",
                    c.Saga.AgvId, c.Saga.CorrelationId, c.Saga.AgvStatus!.BatteryCharge, nameof(Charging)));

        private EventActivityBinder<AgvSaga, AgvStoppedCharging> AgvStoppedChargingReceivedHandler() =>
            When(AgvStoppedChargingEvent)
                .TransitionTo(Idle)
                .Then(c => _logger.LogInformation("{AgvId} / {CorrelationId} reloaded its battery level to {batteryLevel}% => Transitioned to {State}",
                    c.Saga.AgvId, c.Saga.CorrelationId, c.Saga.AgvStatus!.BatteryCharge, nameof(Idle)));
        #endregion
    }
}