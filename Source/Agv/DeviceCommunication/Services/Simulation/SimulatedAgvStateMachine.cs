using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
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
        #region Records
        /// <summary>
        /// Internal message to create and AGV instance
        /// </summary>
        /// <param name="CorrelationId"></param>
        /// <param name="Agv"></param>
        /// <param name="NodeTravelTime"></param>
        public record CreateAgv(Guid CorrelationId, SimulatedAgv Agv, int NodeTravelTime) : CorrelatedBy<Guid>;

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
        /// Internal message to schedule signaling of position
        /// </summary>
        /// <param name="CorrelationId"></param>
        public record ScheduleSignalPosition(Guid CorrelationId) : CorrelatedBy<Guid>;
        #endregion

        #region States
        /// <summary>
        /// AGV is idle
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
        #endregion

        #region Events
        /// <summary>
        /// ExecuteTransportOrder was received
        /// </summary>
        public Event<ExecuteTransportOrder> ExecuteTransportOrderEvent { get; private set; } = default!;

        /// <summary>
        /// CreateAgv was received
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
        /// RequestAgvMap was received
        /// </summary>
        public Event<RequestAgvMap> RequestAgvMapEvent { get; private set; } = default!;
        #endregion

        #region Schedules
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
        /// Trigger for scheduled signalling of position.
        /// </summary>
        public Schedule<SimulatedAgvSaga, ScheduleSignalPosition> SignalPositionSchedule { get; private set; } = default!;
        #endregion

        /// <summary>
        /// Interval for signalling position durting order execution.
        /// </summary>
        public int SignalPositionIntervalMs { get; set; }

        private readonly ILogger<SimulatedAgvStateMachine> _logger;
        private readonly SimulateOptions _simulateOptions;
        private readonly int _sendStatusIntervalMs; // Pause between sending the agv status in [ms]

        /// <summary>
        /// SimulatedAgvStateMachine Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="simulateOptions"></param>
        public SimulatedAgvStateMachine(ILogger<SimulatedAgvStateMachine> logger, SimulateOptions simulateOptions)
        {
            _logger = logger;
            _simulateOptions = simulateOptions;
            _sendStatusIntervalMs = Math.Clamp(_simulateOptions.NodeTravelTime / 2, 100, 10000); // Send twice per node (min 0.1s, max 10s)
            SignalPositionIntervalMs = Math.Clamp(_simulateOptions.NodeTravelTime / 10, 200, 2000); // Send 10x per node (min 0.2s, max 2s)
            _logger.LogInformation("Initialized SimulatedAgvStateMachine with node travel time {NodetravelTime}", _simulateOptions.NodeTravelTime);

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
            DuringAny(SetRequestAgvMapHandler());
            DuringAny(SetSignalPositionHandler());

            SetCompletedWhenFinalized(); // Delete when finalized
        }

        private void SetScheduler()
        {
            Schedule(() => StatusUpdateSchedule,
                s => s.SendStatusUpdateTokenId,
                configuration =>
                {
                    configuration.Delay = TimeSpan.FromMilliseconds(_sendStatusIntervalMs);
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

            Schedule(() => SignalPositionSchedule,
                s => s.SignalPositionTokenId,
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
                e.SetSagaFactory(context => new SimulatedAgvSaga(context.Message.Agv, context.Message.NodeTravelTime))
            );
        }

        private EventActivityBinder<SimulatedAgvSaga, CreateAgv> SetInitialCreateAgvHandler() =>
            When(CreateAgvEvent)
                .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(CreateAgv), c.Message.Agv.AgvId, c.Saga.CorrelationId))
                .TransitionTo(Idle)
                .ThenAsync(PublishStatus)
                .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId));

        /// <summary>
        /// Handler for ExecuteTransportOrder
        /// </summary>
        /// <returns>Event Binding for IHandleHostOrder</returns>
        private EventActivityBinder<SimulatedAgvSaga, ExecuteTransportOrder> SetExecuteTransportOrderHandler() =>
            When(ExecuteTransportOrderEvent)
                .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(ExecuteTransportOrder), c.Message.AgvId, c.Saga.CorrelationId))
                .TransitionTo(BusyMovingToSource)
                .Then(ExecuteOrder)
                .Publish(c => new TransportOrderStarted(c.Message.OrderDetails.CorrelationId, c.Message.OrderDetails.OrderId))
                .ThenAsync(PublishStatus)
                .Schedule(PickOrderSchedule,
                    c => new SchedulePickOrder(c.Saga.CorrelationId),
                    c => c.Saga.TimeToPick!.Value)
                .Schedule(FinishOrderSchedule,
                    c => new ScheduleFinishOrder(c.Saga.CorrelationId),
                    c => c.Saga.TimeToFinish!.Value)
                .Schedule(SignalPositionSchedule,
                    c => new ScheduleSignalPosition(c.Saga.CorrelationId),
                    _ => TimeSpan.FromMilliseconds(SignalPositionIntervalMs));

        private EventActivityBinder<SimulatedAgvSaga, ScheduleStatusUpdate> SetSendAgvStatusHandler() =>
            When(StatusUpdateSchedule.Received)
                .Then(c => _logger.LogDebug("{MessageType} {CorrelationId} received", nameof(ScheduleStatusUpdate), c.Saga.CorrelationId))
                .ThenAsync(PublishStatus)
                .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId));

        private EventActivityBinder<SimulatedAgvSaga, SchedulePickOrder> SetPickOrderHandler() =>
            When(PickOrderSchedule.Received)
                .Then(c => _logger.LogDebug("{MessageType} {CorrelationId} received", nameof(SchedulePickOrder), c.Saga.CorrelationId))
                .TransitionTo(BusyMovingToDestination)
                .Publish(c => new TransportOrderPickCompleted(
                    c.Saga.OrderDetails!.CorrelationId,
                    c.Saga.OrderDetails!.OrderId))
                .ThenAsync(PublishStatus);

        private EventActivityBinder<SimulatedAgvSaga, ScheduleFinishOrder> SetFinishOrderHandler() =>
            When(FinishOrderSchedule.Received)
                .Then(c => _logger.LogDebug("{MessageType} {CorrelationId} received", nameof(ScheduleFinishOrder), c.Saga.CorrelationId))
                .ThenAsync(FinishOrder)
                .TransitionTo(Idle)
                .ThenAsync(PublishStatus);

        private EventActivityBinder<SimulatedAgvSaga, ScheduleSignalPosition> SetSignalPositionHandler() =>
            When(SignalPositionSchedule.Received)
                .Then(c => _logger.LogDebug("{MessageType} {CorrelationId} received", nameof(ScheduleSignalPosition), c.Saga.CorrelationId))
                .If(c => c.Saga.CurrentState != "Idle",
                    t => t.ThenAsync(PublishPosition)
                    .Schedule(SignalPositionSchedule,
                        c => new ScheduleSignalPosition(c.Saga.CorrelationId),
                        _ => TimeSpan.FromMilliseconds(SignalPositionIntervalMs))
                );

        private async Task PublishStatus(BehaviorContext<SimulatedAgvSaga> context)
        {
            _logger.LogTrace("Status event from AGV {AgvId} / {CorrelationId}",
                context.Saga.AgvId, context.Saga.CorrelationId);

            await context.Publish(new AgvStatusReceived(
                context.Saga.CorrelationId,
                context.Saga.AgvId,
                context.Saga.AgvStatus!
            ), context.CancellationToken);
        }

        private async Task PublishPosition(BehaviorContext<SimulatedAgvSaga> context)
        {
            var position = context.Saga.Position;
            if (position != null)
            {
                _logger.LogTrace("Position event from AGV {AgvId} / {CorrelationId}: {Position}",
                    context.Saga.AgvId, context.Saga.CorrelationId,
                    position);

                await context.Publish(new AgvPositionReceived(
                    context.Saga.CorrelationId,
                    context.Saga.AgvId,
                    position
                ), context.CancellationToken);
            }
        }

        private void ExecuteOrder(BehaviorContext<SimulatedAgvSaga, ExecuteTransportOrder> context)
        {
            context.Saga.OrderDetails = context.Message.OrderDetails;
            context.Saga.Nodes = context.Message.Nodes;
            context.Saga.OrderStartDate = DateTime.UtcNow;

            _logger.LogInformation("AGV {AgvId} / {CorrelationId} started order {OrderId} / {OrderCorrelationId}. Order will be picked up at {OrderPickDate:HH:mm:ss.fff} and dropped at {OrderFinishDate:HH:mm:ss.fff}",
                context.Saga.AgvId, context.Saga.CorrelationId,
                context.Message.OrderDetails.OrderId, context.Message.OrderDetails.CorrelationId, context.Saga.OrderPickDate, context.Saga.OrderFinishDate);
        }

        private async Task FinishOrder(BehaviorContext<SimulatedAgvSaga, ScheduleFinishOrder> context)
        {
            var order = context.Saga.OrderDetails;

            if (order == null)
            {
                _logger.LogError("Could not finish order, Order object is missing");
            }
            else
            {
                _logger.LogInformation("AGV {AgvId} / {CorrelationId} finished order {OrderId} / {OrderCorrelationId}",
                    context.Saga.AgvId, context.Saga.CorrelationId, order.OrderId, order.CorrelationId);
                await context.Publish(new TransportOrderCompleted(
                    order.CorrelationId,
                    order.OrderId
                ), context.CancellationToken);
                await PublishPosition(context);
            }

            context.Saga.ResetOrder();
        }

        private EventActivityBinder<SimulatedAgvSaga, RequestAgvGraph> SetRequestAgvGraphHandler() =>
            When(RequestAgvGraphEvent)
                .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestAgvGraph), c.Message.AgvId, c.Saga.CorrelationId))
                .If(c => c.Saga.AgvGraph != null,
                    x =>
                        x.Publish(c => new AgvGraphReceived(
                            c.Saga.CorrelationId,
                            c.Saga.AgvId,
                            c.Saga.AgvGraph!
                        )));

        private EventActivityBinder<SimulatedAgvSaga, RequestAgvData> SetRequestAgvDataHandler() =>
            When(RequestAgvDataEvent)
                .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestAgvData), c.Message.AgvId, c.Saga.CorrelationId))
                .If(c => c.Saga.AgvData != null,
                    x =>
                        x.Publish(c => new AgvDataReceived(
                            c.Saga.CorrelationId,
                            c.Saga.AgvId,
                            c.Saga.AgvData!
                        )));

        private EventActivityBinder<SimulatedAgvSaga, RequestAgvMap> SetRequestAgvMapHandler() =>
            When(RequestAgvMapEvent)
                .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} / {MapId} received", nameof(RequestAgvMap), c.Message.AgvId, c.Saga.CorrelationId, c.Message.MapId))
                .If(c => c.Saga.AgvMap != null,
                    x =>
                        x.Publish(c => new AgvMapReceived(
                            c.Saga.CorrelationId,
                            c.Saga.AgvId,
                            new AgvMap(
                                c.Message.MapCorrelationId,
                                c.Message.MapId,
                                c.Saga.AgvMap!.ModificationDate,
                                c.Saga.AgvMap!.ImageData,
                                c.Saga.AgvMap!.Resolution,
                                c.Saga.AgvMap.XOffset,
                                c.Saga.AgvMap.YOffset
                            )
                        )));
    }
}
