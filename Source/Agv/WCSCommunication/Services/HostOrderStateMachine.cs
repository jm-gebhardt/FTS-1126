using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services;

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


    /// <summary>
    /// Order failed to process
    /// </summary>
    public State Invalid { get; private set; } = default!;

    /// <summary>
    /// Order could not be cancelled
    /// </summary>
    public State CancelFailed { get; private set; } = default!;
    #endregion

    #region Events
    /// <summary>
    /// New order detected. This is the initial event of the HostOrderStateMachine
    /// </summary>
    public Event<CreateHostOrder> CreateHostOrderEvent { get; private set; } = default!;

    /// <summary>
    /// <see cref="ActionGroupInvalid"/> was received
    /// </summary>
    public Event<ActionGroupInvalid> ActionGroupInvalidEvent { get; private set; } = default!;
    /// <summary>
    /// <see cref="ActionGroupCreated"/> was received
    /// </summary>
    public Event<ActionGroupCreated> ActionGroupCreatedEvent { get; private set; } = default!;

    /// <summary>
    /// <see cref="ActionGroupStarted"/> was received
    /// </summary>
    public Event<ActionGroupStarted> ActionGroupStartedEvent { get; private set; } = default!;

    /// <summary>
    /// <see cref="ActionGroupCompleted"/> was received
    /// </summary>
    public Event<ActionGroupCompleted> ActionGroupCompletedEvent { get; private set; } = default!;

    /// <summary>
    /// <see cref="ActionCompletedEvent"/> was received
    /// </summary>
    public Event<ActionCompletedEvent> ActionCompletedEvent { get; private set; } = default!;

    /// <summary>
    /// <see cref="ActionGroupFailed"/> was received
    /// </summary>
    public Event<ActionGroupFailed> ActionGroupFailedEvent { get; private set; } = default!;

    /// <summary>
    /// <see cref="ActionGroupCancelled"/> was received
    /// </summary>
    public Event<ActionGroupCancelled> ActionGroupCancelledEvent { get; private set; } = default!;

    /// <summary>
    /// <see cref="ActionGroupCancelFailed"/> was received
    /// </summary>
    public Event<ActionGroupCancelFailed> ActionGroupCancelFailedEvent { get; private set; } = default!;

    #endregion

    #region Initialization
    /// <summary>
    /// HostOrderStateMachine
    /// </summary>
    public HostOrderStateMachine(ILogger<HostOrderStateMachine> logger, IStateObserver<HostOrderSaga> stateObserver)
    {
        _logger = logger;

        SetSagaFactory();
        InstanceState(x => x.CurrentState);
        ConnectStateObserver(stateObserver);
        Event(() => ActionGroupStartedEvent, x => x.CorrelateBy(ActionGroupId<ActionGroupStarted>()));
        Event(() => ActionGroupCreatedEvent, x => x.CorrelateBy(ActionGroupId<ActionGroupCreated>()));
        Event(() => ActionGroupCompletedEvent, x => x.CorrelateBy(ActionGroupId<ActionGroupCompleted>()));
        Event(() => ActionCompletedEvent, x => x.CorrelateBy((saga, context) => saga.PickActionGuid == context.Message.CorrelationId));
        Event(() => ActionGroupCancelledEvent, x => x.CorrelateBy(ActionGroupId<ActionGroupCancelled>()));
        Event(() => ActionGroupFailedEvent, x => x.CorrelateBy(ActionGroupId<ActionGroupFailed>()));
        Event(() => ActionGroupCancelFailedEvent, x => x.CorrelateBy(ActionGroupId<ActionGroupCancelFailed>()));
        Event(() => ActionGroupInvalidEvent, x => x.CorrelateBy(ActionGroupId<ActionGroupInvalid>()));

        Initially(SetCreateHostOrderHandler());
        During(Initial, ActionGroupInvalidEventHandler());
        During(Initial,ActionGroupCreatedEventHandler());
        DuringAny(ActionGroupStartedEventHandler());
        DuringAny(SetPickCompletedEventHandler());
        DuringAny(ActionGroupCompletedEventHandler());
        DuringAny(ActionGroupCancelFailedEventHandler());
        DuringAny(ActionGroupCanceledEventHandler());
        DuringAny(ActionGroupFailedEventHandler());
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

    private static Expression<Func<HostOrderSaga, ConsumeContext<TMessage>, bool>> ActionGroupId<TMessage>() where TMessage : class, IActionGroupEvent
    {
        return (saga, context) => saga.CorrelationId == context.Message.ActionGroup.CorrelationId;
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
                _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(CreateHostOrder), c.Message.OrderDetails.OrderId, c.Message.OrderDetails.CorrelationId))
            .TransitionTo(Initial)
             .ThenAsync(async c => await c.Publish(new QueueActionGroup(c.Saga.GetPickDropActionGroup())));

    private EventActivityBinder<HostOrderSaga, ActionGroupInvalid> ActionGroupInvalidEventHandler() =>
        When(ActionGroupInvalidEvent)
            .Then(c => _logger.LogInformation("{MessageType} for action group with {CorrelationId} received", nameof(ActionGroupInvalid), c.Message.ActionGroup.CorrelationId))
            .TransitionTo(Invalid);

    private EventActivityBinder<HostOrderSaga, ActionGroupCreated> ActionGroupCreatedEventHandler() =>
        When(ActionGroupCreatedEvent)
            .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(ActionGroupCreated), c.Saga.OrderId, c.Saga.CorrelationId))
            .TransitionTo(OrderQueued);

    private EventActivityBinder<HostOrderSaga, ActionGroupStarted> ActionGroupStartedEventHandler() =>
        When(ActionGroupStartedEvent)
            .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(ActionGroupStarted), c.Saga.OrderId, c.Saga.CorrelationId))
            .Then(c => c.Saga.AssignAgv(c.Message.ActionGroup.AssignedAgv))
            .TransitionTo(Started);

    private EventActivityBinder<HostOrderSaga, ActionCompletedEvent> SetPickCompletedEventHandler() =>
        When(ActionCompletedEvent)
            .Then(c => _logger.LogInformation("Pick action completed for order {OrderId} / {CorrelationId}", c.Saga.OrderId, c.Saga.CorrelationId))
            .TransitionTo(Picked);

    private EventActivityBinder<HostOrderSaga, ActionGroupCompleted> ActionGroupCompletedEventHandler() =>
        When(ActionGroupCompletedEvent)
            .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(ActionGroupCompleted), c.Saga.OrderId, c.Saga.CorrelationId))
            .TransitionTo(Completed);
    private EventActivityBinder<HostOrderSaga, ActionGroupCancelled> ActionGroupCanceledEventHandler() =>
        When(ActionGroupCancelledEvent)
            .Then(c => _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(ActionGroupCancelled), c.Saga.OrderId, c.Saga.CorrelationId))
            .TransitionTo(Cancelled);
    
    private EventActivityBinder<HostOrderSaga, ActionGroupCancelFailed> ActionGroupCancelFailedEventHandler() =>
        When(ActionGroupCancelFailedEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(ActionGroupCancelFailed), c.Saga.OrderId, c.Saga.CorrelationId);
                c.Saga.CancellationError = c.Message.ErrorCode;
            })
            .TransitionTo(CancelFailed);
    private EventActivityBinder<HostOrderSaga, ActionGroupFailed> ActionGroupFailedEventHandler() =>
        When(ActionGroupFailedEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(ActionGroupFailed), c.Saga.OrderId, c.Saga.CorrelationId);
                c.Saga.HandlingError = c.Message.ErrorCode;
            })
            .TransitionTo(Failed);

    #endregion
}