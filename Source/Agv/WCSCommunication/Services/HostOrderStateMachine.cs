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
    /// Sent as soon as the action has been successfully executed by the Agv
    /// </summary>
    [PublicAPI]
    public record ActionStatusChangedEvent(Guid CorrelationId, Guid ParentId = new());

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

            Event(() => ActionStatusChangedEvent, x => x.CorrelateById(context => context.Message.ParentId));
            DuringAny(HandleActionStatusChangedEvent());

            DuringAny(SetActionStartedEventHandler());
            DuringAny(SetActionCompletedEventHandler());
            DuringAny(SetActionFailedEventHandler());
            DuringAny(SetActionCancelledEventHandler());
            DuringAny(SetActionCancelFailedEventHandler());

            OnUnhandledEvent(UnhandledEventCallback);
        }

        #region Test #8348
        /// <summary/>
        public Event<ActionStatusChangedEvent> ActionStatusChangedEvent { get; private set; } = default!;

        private EventActivityBinder<HostOrderSaga, ActionStatusChangedEvent> HandleActionStatusChangedEvent() =>
            When(ActionStatusChangedEvent)
                .Then(c =>
                {
                    _logger.LogInformation("ActionStatusChangedEvent");
                })
                .Publish(c => new HostOrderHandlingSucceeded(c.Saga.CorrelationId, c.Saga.OrderDetails.OrderId));
        #endregion

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

        Task UnhandledEventCallback(UnhandledEventContext<HostOrderSaga> context)
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
            When(ActionStartedEvent)
                .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId))
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
            When(ActionCompletedEvent)
                .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId))
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
            When(ActionFailedEvent)
                .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId));

        private EventActivityBinder<HostOrderSaga, ActionCancelledEvent> SetActionCancelledEventHandler() =>
            When(ActionCancelledEvent)
                .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId));

        private EventActivityBinder<HostOrderSaga, ActionCancelFailedEvent> SetActionCancelFailedEventHandler() =>
            When(ActionCancelFailedEvent)
                .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(c.Message), c.Saga.OrderId, c.Saga.CorrelationId));

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
