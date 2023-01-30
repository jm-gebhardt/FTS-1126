using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Requests;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions
{
    /// <summary>
    /// Handles communication with agv, sends out action and handles its completion.
    /// Finalized as soon as the action has been completed successfully.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class ActionStateMachine : MassTransitStateMachine<ActionSaga>
    {
        private readonly ILogger<ActionStateMachine> _logger;

        /// <summary>
        /// Once the Sage has been created
        /// </summary>
        public State ActionCreated { get; } = default!;

        /// <summary>
        /// Once the StartActionCommand is sent to the agv
        /// </summary>
        public State ActionSend { get; } = default!;

        /// <summary>
        /// Once the TransportOrderCompleted is received
        /// </summary>
        public State ActionCompleted { get; } = default!;

        /// <summary>
        /// Initial event to create the ActionSage once <see cref="ActionCreatedEvent"/> is received
        /// </summary>
        public Event<CreateActionRequest> ActionCreatedEvent { get; private set; } = default!;

        /// <summary>
        /// Event which is triggered when <see cref="AssignActionRequest"/> is received. Then the action should be assigned to this Agv. This must be confirmed with <see cref="ActionAssignmentSucceededEvent"/>
        /// </summary>
        public Event<AssignActionRequest> ActionAssignedEvent { get; private set; } = default!;

        /// <summary>
        /// Event for <see cref="ActionStatusChangedEvent"/>
        /// </summary>
        public Event<ActionStatusChangedEvent> ExecuteActionStatusChangedEvent { get; private set; } = default!;

        /// <summary>
        /// Create new instance of <see cref="ActionStateMachine"/>
        /// </summary>
        public ActionStateMachine(ILogger<ActionStateMachine> logger)
        {
            _logger = logger;
            SetSagaFactory();
            InstanceState(x => x.CurrentState);

            Initially(ActionCreatedHandler());
            During(ActionCreated, ActionAssignedHandler());
            DuringAny(ExecuteActionStatusChangedHandler());
            SetCompletedWhenFinalized();
        }

        private void SetSagaFactory()
        {
            Event(() => ActionCreatedEvent, e =>
                e.SetSagaFactory(context => new ActionSaga(context.Message.CorrelationId, context.Message.Action))
            );
        }

        private async Task SendReleaseAgv(BehaviorContext<ActionSaga, ActionStatusChangedEvent> context)
        {
            await context.Publish(new ReleaseAgv(correlationId: context.Saga.AssignedAgv));
        }

        private EventActivityBinder<ActionSaga, CreateActionRequest> ActionCreatedHandler() =>
            When(ActionCreatedEvent)
                .Then(_ => _logger.LogInformation(nameof(ActionCreatedHandler)))
                .TransitionTo(ActionCreated)
                .Respond(c => new ActionCreationSucceededEvent(c.Message.CorrelationId));

        private EventActivityBinder<ActionSaga, AssignActionRequest> ActionAssignedHandler() =>
            When(ActionAssignedEvent)
                .Then(Validate)
                .Then(UpdateSaga)
                .Publish(c => new AgvAssignmentSucceeded(c.Saga.Action.OrderCorrelationId, c.Saga.AssignedAgvId!, c.Saga.AssignedAgv, c.Message.Action.Nodes))
                .Publish(c => new StartActionCommand(c.Saga.AssignedAgv, c.Saga.AssignedAgvId!, c.Saga.Action))
                .TransitionTo(ActionSend)
                .Respond(c => new ActionAssignmentSucceededEvent(c.Message.CorrelationId));

        private EventActivityBinder<ActionSaga, ActionStatusChangedEvent> ExecuteActionStatusChangedHandler() =>
            When(ExecuteActionStatusChangedEvent)
                .Then(_ =>
                {
                    _logger.LogInformation(nameof(ExecuteActionStatusChangedHandler));
                })
                .ThenAsync(async context =>
                {
                    var action = context.Saga.Action;
                    var orderCorrelationId = action.OrderCorrelationId;
                    var agvCorrelationId = action.AgvCorrelationId;
                    string orderId = action.OrderDetails.OrderId;
                    switch (context.Message.ChangedType)
                    {
                        case ActionStatusChangedType.Started:
                            // to be received in the OrderStateMachine
                            await context.Publish(new TransportOrderStarted(
                                orderCorrelationId,
                                orderId));

                            // to be received in the AgvStateMachine
                            await context.Publish(new ActionStartedEvent(
                                agvCorrelationId));
                            break;

                        case ActionStatusChangedType.Picked:
                            await context.Publish(new TransportOrderPickCompleted(
                                orderCorrelationId,
                                orderId));
                            break;
                        case ActionStatusChangedType.Cancelled:
                            await context.Publish(new TransportOrderCancelSucceeded(
                                orderCorrelationId,
                                orderId));
                            break;
                        case ActionStatusChangedType.CancelFailed:
                            await context.Publish(new TransportOrderCancelFailed(
                                orderCorrelationId,
                                orderId));
                            break;
                        case ActionStatusChangedType.Failed:
                            await context.Publish(new TransportOrderFailed(
                                orderCorrelationId,
                                orderId));
                            break;
                        case ActionStatusChangedType.Dropped:
                        case ActionStatusChangedType.Completed:
                            if (action.ActionType == ActionType.Transport)
                            {
                                await context.Publish(new TransportOrderCompleted(
                                    orderCorrelationId,
                                    orderId
                                ), context.CancellationToken);

                                await SendReleaseAgv(context);
                                await context.TransitionToState(ActionCompleted);
                                await context.TransitionToState(Final);
                            }
                            else
                            {
                                _logger.LogInformation("Completed: {actionType}", action.ActionType);
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                });

        private void Validate(BehaviorContext<ActionSaga, AssignActionRequest> context)
        {
            if (string.IsNullOrEmpty(context.Message.AgvId))
            {
                throw new ArgumentException("Agv not set");
            }
        }

        private void UpdateSaga(BehaviorContext<ActionSaga, AssignActionRequest> context)
        {
            context.Saga.Action = context.Message.Action;
            context.Saga.AssignedAgv = context.Message.Agv;
            context.Saga.AssignedAgvId = context.Message.AgvId;
        }
    }
}