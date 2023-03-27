using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;

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
        /// <see cref="ActionStarted"/> was received
        /// </summary>
        public Event<ActionStartedEvent> ActionStarted { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionCompleted"/> was received
        /// </summary>
        public Event<ActionCompletedEvent> ActionCompleted { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionFailed"/> was received
        /// </summary>
        public Event<ActionFailedEvent> ActionFailed { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionCancelled"/> was received
        /// </summary>
        public Event<ActionCancelledEvent> ActionCancelled { get; private set; } = default!;

        /// <summary>
        /// <see cref="ActionCancelFailed"/> was received
        /// </summary>
        public Event<ActionCancelFailedEvent> ActionCancelFailed { get; private set; } = default!;

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

            Event(() => ActionStarted, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ActionCompleted, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ActionFailed, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ActionCancelled, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ActionCancelFailed, x => x.CorrelateById(context => context.Message.ParentId));

            DuringAny(SetActionStartedEventHandler());
            DuringAny(SetActionCompletedEventHandler());
            DuringAny(SetActionFailedEventHandler());
            DuringAny(SetActionCancelledEventHandler());
            DuringAny(SetActionCancelFailedEventHandler());

            OnUnhandledEvent(UnhandledEventCallback);
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

        Task UnhandledEventCallback(UnhandledEventContext<HostOrderSaga> c)
        {
            _logger.LogInformation("UnhandledEvent");
            return Task.CompletedTask;
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

        private EventActivityBinder<HostOrderSaga, ActionStartedEvent> SetActionStartedEventHandler() =>
            When(ActionStarted)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                )
                .Then(c =>
                {
                    if (c.Message.CorrelationId == c.Saga.PickActionGuid)
                    {
                        _logger.LogInformation("Pick action started for order {OrderId} / {CorrelationId}", c.Saga.OrderId, c.Saga.CorrelationId);

                        c.Publish(new HostOrderStarted(c.Saga.CorrelationId, c.Saga.OrderId, string.Empty)); //  Where does the assignedAgv come from?
                        c.TransitionToState(Started);
                    }
                    else if (c.Message.CorrelationId == c.Saga.DropActionGuid)
                    {
                        _logger.LogInformation("Drop action started for order {OrderId} / {CorrelationId}", c.Saga.OrderId, c.Saga.CorrelationId);
                    }
                    else
                    {
                        _logger.LogWarning("Message {MessageType} for order {OrderId} / {CorrelationId} doesn't belong to the pick nor the drop action",
                            nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId);
                    }
                });

        private EventActivityBinder<HostOrderSaga, ActionCompletedEvent> SetActionCompletedEventHandler() =>
            When(ActionCompleted)
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
                    else if (c.Message.CorrelationId == c.Saga.DropActionGuid)
                    {
                        _logger.LogInformation("Drop action completed for order {OrderId} / {CorrelationId}", c.Saga.OrderId, c.Saga.CorrelationId);
                        c.TransitionToState(Completed);
                    }
                    else
                    {
                        _logger.LogWarning("Message {MessageType} for order {OrderId} / {CorrelationId} doesn't belong to the pick nor the drop action",
                            nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId);
                    }
                });

        private EventActivityBinder<HostOrderSaga, ActionFailedEvent> SetActionFailedEventHandler() =>
            When(ActionFailed)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                );

        private EventActivityBinder<HostOrderSaga, ActionCancelledEvent> SetActionCancelledEventHandler() =>
            When(ActionCancelled)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                );

        private EventActivityBinder<HostOrderSaga, ActionCancelFailedEvent> SetActionCancelFailedEventHandler() =>
            When(ActionCancelFailed)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId)
                );

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
