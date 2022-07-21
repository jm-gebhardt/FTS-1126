using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// State Machine used to track agvs
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvStateMachine: MassTransitStateMachine<AgvSaga>
    {
 
        private readonly ILogger<AgvStateMachine> _logger;

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
        /// The graph of the Agv was received
        /// </summary>
        public Event<AgvGraphReceived> AgvGraphReceivedEvent { get; private set; } = default!;
    
    

        /// <summary>
        /// The data of the Agv was received
        /// </summary>
        public Event<AgvDataReceived> AgvDataReceivedEvent { get; private set; } = default!;

        /// <summary>
        /// If true, AGV are automatically moved from Discovered to Idle
        /// </summary>
        private const bool AutoAcceptDiscoveredAgv = true;

        /// <summary>
        /// Constructor of AgvStateMachine
        /// </summary>
        /// <param name="logger"></param>
        public AgvStateMachine(ILogger<AgvStateMachine> logger)
        {
            _logger = logger;

            SetSagaFactory();
            InstanceState(x => x.CurrentState);
            Initially(InitialAgvStatusReceivedHandler());
        
            During(Discovered, Reserved, Busy, ReleaseAgvHandler());
            During(Idle, AcceptReserveAgvRequestHandler());
            During(Discovered, Reserved, Busy, RejectReserveAgvRequestHandler());
        
            DuringAny(AgvStatusReceivedHandler());
            DuringAny(AgvGraphReceivedHandler());
            DuringAny(AgvDataReceivedHandler());
        }
    
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
        /// First message which creates a new agv saga instance
        /// </summary>
        /// <returns>Event Binding for HandleHostOrder</returns>
        private EventActivityBinder<AgvSaga, AgvStatusReceived> InitialAgvStatusReceivedHandler() =>
            When(AgvStatusReceivedEvent)
                .TransitionTo(Discovered)
                .Then(c =>
                    _logger.LogInformation("New AGV {AgvId}/{CorrelationId} discovered", c.Message.AgvId, c.Saga.CorrelationId))
                // Lets have a auto-discover feature
                .If(_ => AutoAcceptDiscoveredAgv, x => 
                    x.Publish(c => new ReleaseAgv(c.Saga.CorrelationId))
                );

        private EventActivityBinder<AgvSaga, ReserveAgv> AcceptReserveAgvRequestHandler() =>
            When(ReserveAgvEvent)
                .TransitionTo(Reserved)
                .Then(c =>
                    _logger.LogTrace("AGV {AgvId}/{CorrelationId} reserved", c.Saga.AgvId, c.Saga.CorrelationId))
                .Respond(c => new AgvReservationSucceeded(c.Message.CorrelationId));
   
    
        private EventActivityBinder<AgvSaga, ReserveAgv> RejectReserveAgvRequestHandler() =>
            When(ReserveAgvEvent)
                .Then(c => _logger.LogTrace("Could not reserve {AgvId}/{CorrelationId} in state {State}", c.Saga.AgvId, c.Saga.CorrelationId, c.Saga.CurrentState))
                .Respond(c => new AgvReservationRejected(c.Message.CorrelationId));

        private EventActivityBinder<AgvSaga, AgvStatusReceived> AgvStatusReceivedHandler() =>
            When(AgvStatusReceivedEvent)
                .Then(c =>
                    _logger.LogTrace("AgvStatus {AgvId}/{CorrelationId} received", c.Message.AgvId, c.Saga.CorrelationId))
                .Then(c => c.Saga.AgvStatus = c.Message.AgvStatus);

        private EventActivityBinder<AgvSaga, AgvGraphReceived> AgvGraphReceivedHandler() =>
            When(AgvGraphReceivedEvent)
                .Then(c =>
                    _logger.LogTrace("AgvGraph {AgvId}/{CorrelationId} received", c.Message.AgvId, c.Saga.CorrelationId))
                .Then(c => c.Saga.AgvGraph = c.Message.Graph);

        private EventActivityBinder<AgvSaga, AgvDataReceived> AgvDataReceivedHandler() =>
            When(AgvDataReceivedEvent)
                .Then(c =>
                    _logger.LogTrace("AgvData {AgvId}/{CorrelationId} received", c.Message.AgvId, c.Saga.CorrelationId))
                .Then(c => c.Saga.AgvData = c.Message.Data);

        private EventActivityBinder<AgvSaga, ReleaseAgv> ReleaseAgvHandler() =>
            When(ReleaseAgvEvent)
                .TransitionTo(Idle)
                .Then(c =>
                    _logger.LogTrace("Agv {AgvId}/{CorrelationId} released and transitioned to Idle", c.Saga.AgvId, c.Saga.CorrelationId));
    }
}
