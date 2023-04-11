using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services
{
    /// <summary>
    /// Host order state machine
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public sealed class HostOrderStateMachine : MassTransitStateMachine<HostOrderSaga>
    {
        private readonly ILogger<HostOrderStateMachine> _logger;

        #region States

        /// <summary>
        /// Order has been split into actions and actions have been queued into action manager
        /// </summary>
        public State OrderQueued { get; private set; } = default!;

        /// <summary>
        /// Order has been started by any AGV
        /// </summary>
        public State Started { get; private set; } = default!;

        /// <summary>
        /// Pick action has been completed, i.e. the item has been picked
        /// </summary>
        public State Picked { get; private set; } = default!;

        /// <summary>
        /// All actions has been completed
        /// </summary>
        public State Completed { get; private set; } = default!;

        /// <summary>
        /// Order failed to process
        /// </summary>
        public State Failed { get; private set; } = default!;

        /// <summary>
        /// Order has been cancelled
        /// </summary>
        public State Cancelled { get; private set; } = default!;

        #endregion

        #region Events

        /// <summary>
        /// New order detected. This is the initial event of the HostOrderStateMachine
        /// </summary>
        public Event<CreateHostOrder> CreateHostOrderEvent { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionStartedEvent"/> was received
        /// </summary>
        public Event<ActionStartedEvent> ActionStartedEvent { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionCompletedEvent"/> was received
        /// </summary>
        public Event<ActionCompletedEvent> ActionCompletedEvent { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionFailedEvent"/> was received
        /// </summary>
        public Event<ActionFailedEvent> ActionFailedEvent { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionCancelledEvent"/> was received
        /// </summary>
        public Event<ActionCancelledEvent> ActionCancelledEvent { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionCancelFailedEvent"/> was received
        /// </summary>
        public Event<ActionCancelFailedEvent> ActionCancelFailedEvent { get; private set; } = default!;

        #endregion

        #region Initialization

        /// <summary>
        /// HostOrderStateMachine
        /// </summary>
        public HostOrderStateMachine(ILogger<HostOrderStateMachine> logger)
        {
            _logger = logger;

            SetSagaFactory();
            InstanceState(x => x.CurrentState);

            Initially(SetCreateHostOrderHandler());

            Event(() => ActionStartedEvent, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ActionCompletedEvent, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ActionFailedEvent, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ActionCancelledEvent, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ActionCancelFailedEvent, x => x.CorrelateById(context => context.Message.ParentId));

            During(Initial, SetPickStartedEventHandler());
            During(Started, SetDropStartedEventHandler());
            During(Started, SetPickCompletedEventHandler());
            During(Picked, SetDropCompletedEventHandler());
            During(Initial, Started, Picked, SetActionFailedEventHandler());
            During(Initial, Started, Picked, SetActionCancelledEventHandler());
            During(Initial, Started, Picked, SetActionCancelFailedEventHandler());
        }

        /// <summary>
        /// Set saga factory on <see cref="CreateHostOrder"/>
        /// </summary>
        private void SetSagaFactory()
        {
            // Initial creation
            Event(() => CreateHostOrderEvent, e =>
                e.SetSagaFactory(c => new HostOrderSaga(c.Message.OrderDetails))
            );

        }

        #endregion

        #region Transitions

        /// <summary>
        /// Handler for HandleHostOrder
        /// </summary>
        /// <returns>Event Binding for HandleHostOrder</returns>
        private EventActivityBinder<HostOrderSaga, CreateHostOrder> SetCreateHostOrderHandler() =>
            When(CreateHostOrderEvent)
                .Then(c =>
                {
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(CreateHostOrder), c.Message.OrderDetails.OrderId, c.Message.OrderDetails.CorrelationId);
                })
                .TransitionTo(Initial)
                .Then(c => c.Publish(new ActionGroup(c.Saga.CorrelationId, GetPickDropActions(c.Saga))));

        #endregion

        #region Message Handlers

        private EventActivityBinder<HostOrderSaga, ActionStartedEvent> SetPickStartedEventHandler() =>
            When(ActionStartedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                )
                .Then(c =>
                {
                    if (c.Message.CorrelationId == c.Saga.PickActionGuid)
                    {
                        _logger.LogInformation("Pick action started for order {OrderId} / {CorrelationId}", c.Saga.OrderId, c.Saga.CorrelationId);

                        c.Publish(new HostOrderStarted(c.Saga.CorrelationId, c.Saga.OrderId, c.Message.AgvId));
                        c.TransitionToState(Started);
                    }
                    else
                    {
                        _logger.LogInformation("Message {MessageType} for order {OrderId} / {CorrelationId} is not handled", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId);
                    }
                });

        private EventActivityBinder<HostOrderSaga, ActionStartedEvent> SetDropStartedEventHandler() =>
            When(ActionStartedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                )
                .Then(c =>
                {
                    if (c.Message.CorrelationId == c.Saga.DropActionGuid)
                    {
                        _logger.LogInformation("Drop action started for order {OrderId} / {CorrelationId}", c.Saga.OrderId, c.Saga.CorrelationId);
                    }
                    else
                    {
                        _logger.LogInformation("Message {MessageType} for order {OrderId} / {CorrelationId} is not handled", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId);
                    }
                });

        private EventActivityBinder<HostOrderSaga, ActionCompletedEvent> SetPickCompletedEventHandler() =>
            When(ActionCompletedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                )
                .Then(c =>
                {
                    if (c.Message.CorrelationId == c.Saga.PickActionGuid)
                    {
                        _logger.LogInformation("Pick action completed for order {OrderId} / {CorrelationId}", c.Saga.OrderId, c.Saga.CorrelationId);
                        c.TransitionToState(Picked);
                    }
                    else
                    {
                        _logger.LogInformation("Message {MessageType} for order {OrderId} / {CorrelationId} is not handled", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId);
                    }
                });

        private EventActivityBinder<HostOrderSaga, ActionCompletedEvent> SetDropCompletedEventHandler() =>
            When(ActionCompletedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                )
                .Then(c =>
                {
                    if (c.Message.CorrelationId == c.Saga.DropActionGuid)
                    {
                        _logger.LogInformation("Drop action completed for order {OrderId} / {CorrelationId}", c.Saga.OrderId, c.Saga.CorrelationId);
                        c.TransitionToState(Completed);
                    }
                    else
                    {
                        _logger.LogInformation("Message {MessageType} for order {OrderId} / {CorrelationId} is not handled", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId);
                    }
                });

        private EventActivityBinder<HostOrderSaga, ActionFailedEvent> SetActionFailedEventHandler() =>
            When(ActionFailedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                )
                .TransitionTo(Failed);

        private EventActivityBinder<HostOrderSaga, ActionCancelledEvent> SetActionCancelledEventHandler() =>
            When(ActionCancelledEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                )
                .TransitionTo(Cancelled);

        private EventActivityBinder<HostOrderSaga, ActionCancelFailedEvent> SetActionCancelFailedEventHandler() =>
            When(ActionCancelFailedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                )
                .TransitionTo(Failed);

        #endregion

        #region Private Methods
        private List<AgvAction> GetPickDropActions(HostOrderSaga hostOrderSaga)
        {
            var pickAction = new AgvAction(hostOrderSaga.PickActionGuid, string.Empty, ActionType.Pick, hostOrderSaga.OrderId, hostOrderSaga.CorrelationId, null, hostOrderSaga.OrderDetails.Source);
            var dropAction = new AgvAction(hostOrderSaga.DropActionGuid, string.Empty, ActionType.Drop, hostOrderSaga.OrderId, hostOrderSaga.CorrelationId, hostOrderSaga.OrderDetails.Source, hostOrderSaga.OrderDetails.Destination);
            return new List<AgvAction>()
            {
                pickAction,
                dropAction
            };
        }
        #endregion
    }
}
