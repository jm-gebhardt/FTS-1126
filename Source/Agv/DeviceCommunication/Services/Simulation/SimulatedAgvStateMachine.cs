using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation
{
    /// <summary>
    /// StateMachine to simulate an AGV
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class SimulatedAgvStateMachine : MassTransitStateMachine<SimulatedAgvSaga>
    {
        /// <summary>
        /// Internal message to create and AGV instance
        /// </summary>
        /// /// <param name="CorrelationId"></param>
        /// <param name="Agv"></param>
        public record CreateAgv(Guid CorrelationId, IAgv Agv) : CorrelatedBy<Guid>;
        
        /// <summary>
        /// Internal message to schedule sending status update
        /// </summary>
        /// <param name="CorrelationId"></param>
        public record ScheduleStatusUpdate(Guid CorrelationId) : CorrelatedBy<Guid>;
        
        /// <summary>
        /// Internal message to schedule pick up of the order
        /// </summary>
        /// <param name="CorrelationId"></param>
        public record SchedulePickOrder(Guid CorrelationId) : CorrelatedBy<Guid>;
        
        /// <summary>
        /// Internal message to schedule finishing the order
        /// </summary>
        /// <param name="CorrelationId"></param>
        public record ScheduleFinishOrder(Guid CorrelationId) : CorrelatedBy<Guid>;
        
        /// <summary>
        /// AGV is Idle
        /// </summary>
        public State Idle { get; private set; } = default!;

        /// <summary>
        /// AGV is busy and is moving to source
        /// </summary>
        public State BusyMovingToSource { get; private set; } = default!;
        
        /// <summary>
        /// AGV is busy and is moving to destination
        /// </summary>
        public State BusyMovingToDestination { get; private set; } = default!;


        /// <summary>
        /// IExecuteTransportOrder was received
        /// </summary>
        public Event<ExecuteTransportOrder> ExecuteTransportOrderEvent { get; private set; } = default!;

        /// <summary>
        /// IExecuteTransportOrder was received
        /// </summary>
        public Event<CreateAgv> CreateAgvEvent { get; private set; } = default!;

        /// <summary>
        /// RequestAgvGraph was received
        /// </summary>
        public Event<RequestAgvGraph> RequestAgvGraphEvent { get; private set; } = default!;
    
        /// <summary>
        /// RequestAgvData was received
        /// </summary>
        public Event<RequestAgvData> RequestAgvDataEvent { get; private set; } = default!;

        /// <summary>
        /// Status Schedule triggered
        /// </summary>
        public Schedule<SimulatedAgvSaga, ScheduleStatusUpdate> StatusUpdateSchedule { get; private set; } = default!;
        
        /// <summary>
        /// Finish Order Schedule triggered
        /// </summary>
        public Schedule<SimulatedAgvSaga, SchedulePickOrder> PickOrderSchedule { get; private set; } = default!;
        
        /// <summary>
        /// Finish Order Schedule triggered
        /// </summary>
        public Schedule<SimulatedAgvSaga, ScheduleFinishOrder> FinishOrderSchedule { get; private set; } = default!;

        /// <summary>
        /// Timeout between sending status messages
        /// </summary>
        public const int SendStatusTimeout = 30;
        
        private readonly ILogger<SimulatedAgvStateMachine> _logger;

        /// <summary>
        /// SimulatedAgvStateMachine Constructor
        /// </summary>
        /// <param name="logger"></param>
        public SimulatedAgvStateMachine(ILogger<SimulatedAgvStateMachine> logger)
        {
            _logger = logger;

            SetSagaFactory();
            SetScheduler();
            InstanceState(x => x.CurrentState);

            Initially(SetInitialCreateAgvHandler());
            During(Idle, SetExecuteTransportOrderHandler());
            During(BusyMovingToSource, SetPickOrderHandler());
            During(BusyMovingToDestination, SetFinishOrderHandler());
        
            DuringAny(SetSendAgvStatusHandler());
            DuringAny(SetRequestAgvGraphHandler());
            DuringAny(SetRequestAgvDataHandler());
        
            SetCompletedWhenFinalized(); // Delete when finalized
        }

        private void SetScheduler()
        {
            Schedule(() => StatusUpdateSchedule,
                s => s.SendStatusUpdateTokenId,
                configuration =>
                {
                    configuration.Delay = TimeSpan.FromSeconds(SendStatusTimeout);
                    configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
                });
            
            Schedule(() => PickOrderSchedule,
                s => s.PickOrderTokenId,
                configuration =>
                {
                    configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
                });

            Schedule(() => FinishOrderSchedule,
                s => s.FinishOrderTokenId,
                configuration =>
                {
                    configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
                });
        }

        /// <summary>
        /// Set saga factory on HandleHostOrderEvent
        /// </summary>
        private void SetSagaFactory()
        {
            Event(() => CreateAgvEvent, e =>
                e.SetSagaFactory(context => new SimulatedAgvSaga(context.Message.Agv))
            );
        }

        private EventActivityBinder<SimulatedAgvSaga, CreateAgv> SetInitialCreateAgvHandler() =>
            When(CreateAgvEvent)
                .TransitionTo(Idle)
                .ThenAsync(PublishStatus)
                .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId));

        /// <summary>
        /// Handler for IHandleHostOrder
        /// </summary>
        /// <returns>Event Binding for IHandleHostOrder</returns>
        private EventActivityBinder<SimulatedAgvSaga, ExecuteTransportOrder> SetExecuteTransportOrderHandler() =>
            When(ExecuteTransportOrderEvent)
                .TransitionTo(BusyMovingToSource)
                .Then(ExecuteOrder)
                .Publish(c => new TransportOrderStarted(c.Message.CorrelationId, c.Message.Order.OrderId))
                .ThenAsync(PublishStatus)
                .Schedule(PickOrderSchedule, 
                    c => new SchedulePickOrder(c.Saga.CorrelationId),
                    c =>  c.Saga.TimeToPick!.Value)
                .Schedule(FinishOrderSchedule, 
                    c => new ScheduleFinishOrder(c.Saga.CorrelationId),
                    c =>  c.Saga.TimeToFinish!.Value);
        
        private EventActivityBinder<SimulatedAgvSaga, ScheduleStatusUpdate> SetSendAgvStatusHandler() =>
            When(StatusUpdateSchedule.Received)
                .ThenAsync(PublishStatus)
                .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId));
        
        private EventActivityBinder<SimulatedAgvSaga, SchedulePickOrder> SetPickOrderHandler() =>
            When(PickOrderSchedule.Received)
                .TransitionTo(BusyMovingToDestination)
                .Publish( c => new TransportOrderPickCompleted(
                    c.Saga.Order!.CorrelationId,
                    c.Saga.Order!.OrderId))
                .ThenAsync(PublishStatus);
        
        private EventActivityBinder<SimulatedAgvSaga, ScheduleFinishOrder> SetFinishOrderHandler() =>
            When(FinishOrderSchedule.Received)
                .ThenAsync(FinishOrder)
                .TransitionTo(Idle)
                .ThenAsync(PublishStatus);
        
        private async Task PublishStatus(BehaviorContext<SimulatedAgvSaga> context)
        {
            await context.Publish( new AgvStatusReceived(
                context.Saga.CorrelationId, 
                context.Saga.AgvId,
                context.Saga.AgvStatus!
            ), context.CancellationToken);
        }

        private void ExecuteOrder(BehaviorContext<SimulatedAgvSaga, ExecuteTransportOrder> context)
        {
            context.Saga.Order = context.Message.Order;
            context.Saga.Nodes = context.Message.Nodes;
            context.Saga.OrderStartDate = DateTime.Now;

            _logger.LogInformation("Agv {AgvId}/{CorrelationId} started Order {OrderId}/{OrderCorrelationId}. Order will be picked up at {OrderPickDate} and dropped at {OrderFinishDate}",
                context.Saga.AgvId, context.Saga.CorrelationId,
                context.Message.Order.OrderId, context.Message.Order.CorrelationId, context.Saga.OrderPickDate, context.Saga.OrderFinishDate);
 
        }

        private async Task FinishOrder(BehaviorContext<SimulatedAgvSaga, ScheduleFinishOrder> context)
        {
            var order = context.Saga.Order;

            if (order == null)
            {
                _logger.LogError("Could not finish order, Order object is missing");
            
            }
            else
            {
                _logger.LogInformation("Agv {AgvId}/{CorrelationId} finished Order {OrderId}/{OrderCorrelationId}",
                    context.Saga.AgvId, context.Saga.CorrelationId, order.OrderId, order.CorrelationId);
                await context.Publish(new TransportOrderCompleted(
                    order.CorrelationId,
                    order.OrderId
                ), context.CancellationToken);
            }
        
            context.Saga.ResetOrder();
        } 

        private EventActivityBinder<SimulatedAgvSaga, RequestAgvGraph> SetRequestAgvGraphHandler() =>
            When(RequestAgvGraphEvent)
                .If(c => c.Saga.AgvGraph != null,
                    x => 
                        x.Publish(c => new AgvGraphReceived(
                            c.Saga.CorrelationId,
                            c.Saga.AgvId,
                            c.Saga.AgvGraph!
                        )));
    
        private EventActivityBinder<SimulatedAgvSaga, RequestAgvData> SetRequestAgvDataHandler() =>
            When(RequestAgvDataEvent)
                .If(c => c.Saga.AgvData != null,
                    x => 
                        x.Publish(c => new AgvDataReceived(
                            c.Saga.CorrelationId,
                            c.Saga.AgvId,
                            c.Saga.AgvData!
                        )));
    }
}
