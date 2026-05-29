using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Messages.ActionGroups.Vehicle;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.HeadIds;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Sagas;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using State = MassTransit.State;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.StateMachines;

/// <summary>
/// Represents the state machine for the order execution saga.
/// </summary>
[PublicAPI]
public class OrderExecutionStateMachine : MassTransitStateMachine<OrderExecutionSaga>
{
    private readonly ILogger<OrderExecutionStateMachine> _logger;
    private readonly IMapper _mapper;
    private readonly IOrderActionStatusCacheProvider _orderActionStatusCache;

    #region states
    /// <summary>
    /// Order is in state created.
    /// </summary>
    public State Created { get; private set; } = null!;

    /// <summary>
    /// The order failed.
    /// </summary>
    public State Failed { get; private set; } = null!;

    /// <summary>
    /// Order is in state running (agv confirmed).
    /// </summary>
    public State Running { get; private set; } = null!;

    /// <summary>
    /// Order is in start restarting
    /// </summary>
    public State Restarting { get; private set; } = null!;
    #endregion

    #region events
    /// <summary>
    /// The start order command.
    /// </summary>
    public Event<StartOrder> StartOrderCommand { get; private set; } = null!;

    /// <summary>
    /// The order state received event.
    /// </summary>
    public Event<OrderStateReceived> OrderStateReceivedEvent { get; private set; } = null!;

    /// <summary>
    /// The stop charging requested event.
    /// </summary>
    public Event<StopChargingRequested> StopChargingRequestedEvent { get; private set; } = null!;

    /// <summary>
    /// The order cancellation requested event.
    /// </summary>
    public Event<OrderCancellationRequested> OrderCancellationRequested { get; private set; } = null!;

    /// <summary>
    /// The order update requested event.
    /// </summary>
    public Event<OrderUpdateRequested> OrderUpdateRequestedEvent { get; private set; } = null!;

    /// <summary>
    /// The restart order command.
    /// </summary>
    public Event<RestartOrder> RestartOrderCommand { get; private set; } = null!;

    /// <summary>
    /// The resend order request.
    /// </summary>
    public Event<OrderResendRequested> ResendOrderRequestEvent { get; private set; } = null!;
    #endregion

    /// <inheritdoc/>
    public OrderExecutionStateMachine(ILogger<OrderExecutionStateMachine> logger, ISequenceCounter<IOrderHeaderId> orderHeaderId, ISequenceCounter<IInstantActionsHeaderId> instantActionHeaderId, IOrderActionStatusCacheProvider orderActionStatusCache, IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _orderActionStatusCache = orderActionStatusCache;

        SetSagaFactory(orderHeaderId, instantActionHeaderId);
        InstanceState(x => x.CurrentState);

        Initially(StartOrder());
        During(Created, Running, UpdateOrderState());
        During(Created, Running, StopCharging());
        During(Created, Running, ResendOrder());
        During(Created, Running, UpdateOrder());
        During(Created, Running, Restarting, ResendOnDuplicateStartOrder());
        DuringAny(CancelOrder());
        During(Restarting, RestartOrder());
        SetCompletedWhenFinalized();
    }

    private void SetSagaFactory(ISequenceCounter<IOrderHeaderId> orderHeaderId, ISequenceCounter<IInstantActionsHeaderId> instantActionHeaderId)
    {
        Event(() => StartOrderCommand, e =>
            e.SetSagaFactory(context => new OrderExecutionSaga(context.Message.CorrelationId, context.Message.Order, orderHeaderId, instantActionHeaderId))
        );
    }

    #region message handlers
    private EventActivityBinder<OrderExecutionSaga, StartOrder> StartOrder()
    {
        return When(StartOrderCommand)
            .Then(c => { _logger.LogInformation("{MessageType} for order {OrderCorrelationId} of agv {AgvCorrelationId} received", nameof(StartOrderCommand), c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
            .IfElse(c => c.Saga.Initialize(),
                initialized => initialized
                    .ThenAsync(PublishOrder)
                    .TransitionTo(Created),
                failed => failed
                    .Then(c => { _logger.LogError("Initialization of order {OrderCorrelationId} of agv {AgvCorrelationId} failed: {Reason}", c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId, c.Saga.ErrorReference); })
                    .ThenAsync(PublishOrderFailedEvent)
                    .TransitionTo(Failed)
                    .Finalize()
            );
    }

    private EventActivityBinder<OrderExecutionSaga, OrderStateReceived> UpdateOrderState()
    {
        return When(OrderStateReceivedEvent)
            .Then(c => { _logger.LogInformation("{MessageType} for order {OrderCorrelationId} of agv {AgvCorrelationId} received", nameof(OrderStateReceived), c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
            .IfElse(c => c.Saga.TryUpdateState(c.Message.OrderState),
                updated => updated
                    .ThenAsync(PublishOrderActionStatusChange)
                    .If(c => c.Saga.ShouldSendNextOrder, e => e.ThenAsync(PublishOrder))
                    .If(c => c.Saga.NewBaseRequested, e => e.ThenAsync(PublishNewBaseRequestedEvent))
                    .If(c => c.Saga.OrderStartedRunning, e => e.ThenAsync(PublishOrderStartedEvent).TransitionTo(Running))
                    .If(c => c.Saga.IsFinished, e => e.ThenAsync(PublishOrderFinishedEvent).TransitionTo(Final).Finalize()),
                failed => failed
                    .Then(c => { _logger.LogError("Update of order status {OrderCorrelationId} of agv {AgvCorrelationId} failed: {Reason}", c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId, c.Saga.ErrorReference); })
                    .ThenAsync(PublishOrderFailedEvent)
                    .TransitionTo(Failed)
                    .Finalize()
            );
    }

    private EventActivityBinder<OrderExecutionSaga, StopChargingRequested> StopCharging()
    {
        return When(StopChargingRequestedEvent)
            .Then(c => { _logger.LogInformation("{MessageType} for order {OrderCorrelationId} of agv {AgvCorrelationId} received", nameof(StopChargingRequestedEvent), c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
            .ThenAsync(StopCharging);
    }

    private EventActivityBinder<OrderExecutionSaga, OrderCancellationRequested> CancelOrder()
    {
        return When(OrderCancellationRequested)
            .Then(c => { _logger.LogInformation("{MessageType} for order {OrderCorrelationId} of agv {AgvCorrelationId} received", nameof(OrderCancellationRequested), c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
            .ThenAsync(CancelOrder);
    }

    private EventActivityBinder<OrderExecutionSaga, OrderUpdateRequested> UpdateOrder()
    {
        return When(OrderUpdateRequestedEvent)
            .Then(c => { _logger.LogInformation("{MessageType} for order {OrderCorrelationId} of agv {AgvCorrelationId} received", nameof(OrderUpdateRequested), c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
            .IfElse(c => c.Saga.TryUpdate(c.Message.Order, c.Message.CurrentFleetManagerActionId ?? c.Message.CorrelationId),
                updated => updated
                    .ThenAsync(PublishOrder),
                failed => failed
                    .Then(c => { _logger.LogError("Update of order {OrderCorrelationId} of agv {AgvCorrelationId} failed: {Reason}", c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId, c.Saga.ErrorReference); })
                    .IfElse(c => string.Equals(c.Saga.ErrorReference, OrderErrors.OrderRouteUpdateOrderCompleted.Description),
                        orderCompleted => orderCompleted
                            .TransitionTo(Restarting).Then(c => { _logger.LogError("Order completed {OrderCorrelationId} of agv {AgvCorrelationId} during route update, restarting..", c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
                            .ThenAsync(c => c.Publish(new RestartOrder(c.Saga.CorrelationId))),
                        otherFailure => otherFailure.ThenAsync(PublishOrderFailedEvent))
            );
    }

    private EventActivityBinder<OrderExecutionSaga, RestartOrder> RestartOrder()
    {
        return When(RestartOrderCommand)
            .Then(c => { _logger.LogInformation("{MessageType} for order {OrderCorrelationId} of agv {AgvCorrelationId} received", nameof(RestartOrder), c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
            .IfElse(c => c.Saga.Restart(),
                restarted => restarted
                    .ThenAsync(PublishOrder)
                    .TransitionTo(Created),
                failed => failed
                    .Then(c => { _logger.LogError("Restarting of order {OrderCorrelationId} of agv {AgvCorrelationId} failed: {Reason}", c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId, c.Saga.ErrorReference); })
                    .ThenAsync(PublishOrderFailedEvent)
                    .TransitionTo(Failed)
                    .Finalize()
            );
    }

    private EventActivityBinder<OrderExecutionSaga, OrderResendRequested> ResendOrder()
    {
        return When(ResendOrderRequestEvent)
            .Then(c => { _logger.LogInformation("{MessageType} for order {OrderCorrelationId} of agv {AgvCorrelationId} received", nameof(OrderResendRequested), c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
            .ThenAsync(PublishOrder);
    }

    private EventActivityBinder<OrderExecutionSaga, StartOrder> ResendOnDuplicateStartOrder()
    {
        return When(StartOrderCommand)
            .Then(c => { _logger.LogInformation("{MessageType} for already-active order {OrderCorrelationId} of agv {AgvCorrelationId} received: republishing order so the AGV can resume", nameof(StartOrderCommand), c.Message.CorrelationId, c.Saga.Order.AssignedAgv?.CorrelationId); })
            .ThenAsync(PublishOrder);
    }

    private async Task CancelOrder(BehaviorContext<OrderExecutionSaga> context)
    {
        context.Saga.PrepareInstantAction();
        _logger.LogInformation("Publishing '{ActionType}' instant action {Action} with {HeaderId} for order {ActionCorrelationId} of agv {AgvCorrelationId}", Vda5050Specifications.ActionTypes.CancelOrder, context.Saga.InstantActionId, context.Saga.InstantActionsHeaderId.Current, context.Saga.CorrelationId, context.Saga.Order.AssignedAgv?.CorrelationId);
        await context.PublishMqtt(new MqttInstantActions()
        {
            HeaderId = context.Saga.InstantActionsHeaderId.Current,
            Timestamp = DateTimeOffset.UtcNow,
            Version = Vda5050Specifications.Version,
            Manufacturer = context.Saga.Order.AssignedAgv!.Manufacturer,
            SerialNumber = context.Saga.Order.AssignedAgv!.SerialNumber,
            Actions =
            [
                new MqttInstantAction
                {
                    ActionType = Vda5050Specifications.ActionTypes.CancelOrder,
                    ActionId = context.Saga.InstantActionId!,
                    BlockingType = BlockingType.HARD
                }
            ]
        }, context.Saga.Order.AssignedAgv!.GetRoutingKey(Vda5050Specifications.Subtopics.InstantActions), context.CancellationToken);
    }

    private async Task StopCharging(BehaviorContext<OrderExecutionSaga> context)
    {
        context.Saga.PrepareInstantAction();
        _logger.LogInformation("Publishing '{ActionType}' instant action {Action} with {HeaderId} for order {ActionCorrelationId} of agv {AgvCorrelationId}", Vda5050Specifications.ActionTypes.StopCharging, context.Saga.InstantActionId, context.Saga.InstantActionsHeaderId.Current, context.Saga.CorrelationId, context.Saga.Order.AssignedAgv?.CorrelationId);
        await context.PublishMqtt(new MqttInstantActions()
        {
            HeaderId = context.Saga.InstantActionsHeaderId.Current,
            Timestamp = DateTimeOffset.UtcNow,
            Version = Vda5050Specifications.Version,
            Manufacturer = context.Saga.Order.AssignedAgv!.Manufacturer,
            SerialNumber = context.Saga.Order.AssignedAgv!.SerialNumber,
            Actions =
            [
                new MqttInstantAction
                {
                    ActionType = Vda5050Specifications.ActionTypes.StopCharging,
                    ActionId = context.Saga.InstantActionId!,
                    BlockingType = BlockingType.SOFT
                }
            ]
        }, context.Saga.Order.AssignedAgv!.GetRoutingKey(Vda5050Specifications.Subtopics.InstantActions), context.CancellationToken);
    }

    private async Task PublishOrderActionStatusChange(BehaviorContext<OrderExecutionSaga> context)
    {
        _orderActionStatusCache.ConcurrentDict.TryGetValue(context.Saga.CorrelationId, out IList<OrderActionState>? previousOrderActionStatus);
        if (context.Saga.Order.Status == null || context.Saga.Order.Status.ActionStates.Count == 0)
        {
            return;
        }
        ICollection<Task> publishTasks = [];
        foreach (var actionState in context.Saga.Order.Status.ActionStates)
        {
            var previousState = previousOrderActionStatus?.SingleOrDefault(a => a.ActionId == actionState.ActionId);
            if (previousState == null || previousState.ActionStatus != actionState.ActionStatus)
            {
                publishTasks.Add(context.Publish(new VehicleActionStatusChanged(context.Saga.CorrelationId, context.Saga.Order.AssignedAgv!.CorrelationId, actionState.ActionType ?? string.Empty, actionState.ActionStatus)));

                if (actionState.ActionId == context.Saga.InstantActionId && actionState.ActionStatus == VehicleActionStatus.Finished)
                {
                    publishTasks.Add(PublishOrderCanceledEvent(context));
                    context.Saga.ResetInstantAction();
                }
                if (actionState.ActionId == context.Saga.InstantActionId && actionState.ActionStatus == VehicleActionStatus.Failed)
                {
                    publishTasks.Add(PublishOrderCancelFailedEvent(context));
                    context.Saga.ResetInstantAction();
                }
            }
        }
        _orderActionStatusCache.ConcurrentDict.AddOrUpdate(context.Saga.CorrelationId, context.Saga.Order.Status.ActionStates, (_, _) => context.Saga.Order.Status.ActionStates);
        await Task.WhenAll(publishTasks);
    }

    private async Task PublishOrder(BehaviorContext<OrderExecutionSaga> context)
    {
        if (context.Saga.RouteUpdateRequested)
        {
            _logger.LogInformation("Not publishing {Message}/{HeaderId} for order {OrderCorrelationId}/{OrderId}/{OrderUpdatedId} of agv {AgvCorrelationId}, route update requested", nameof(MqttOrder), context.Saga.OrderHeaderId.Current, context.Saga.CorrelationId, context.Saga.Order.OrderId, context.Saga.Order.OrderUpdateId, context.Saga.Order.AssignedAgv?.CorrelationId);
            return;
        }
        context.Saga.OrderHeaderId.Increase();
        _logger.LogInformation("Publishing {Message}/{HeaderId} for order {OrderCorrelationId}/{OrderId}/{OrderUpdatedId} of agv {AgvCorrelationId}", nameof(MqttOrder), context.Saga.OrderHeaderId.Current, context.Saga.CorrelationId, context.Saga.Order.OrderId, context.Saga.Order.OrderUpdateId, context.Saga.Order.AssignedAgv?.CorrelationId);
        var orderMessage = _mapper.Map<MqttOrder>(context.Saga.Order);
        orderMessage.HeaderId = context.Saga.OrderHeaderId.Current;
        orderMessage.Timestamp = DateTimeOffset.UtcNow;
        await context.PublishMqtt(orderMessage, context.Saga.Order.AssignedAgv!.GetRoutingKey(Vda5050Specifications.Subtopics.Order), context.CancellationToken);
    }

    private async Task PublishOrderStartedEvent(BehaviorContext<OrderExecutionSaga> context)
    {
        await context.Publish(new ActionStartedByAgv(context.Saga.CurrentFleetManagerActionId, context.Saga.Order.AssignedAgv!.CorrelationId));
        var logEntry = new LogEntryDto
        {
            ActionGroupCorrelationId = context.Saga.CorrelationId,
            AgvCorrelationId = context.Saga.Order.AssignedAgv!.CorrelationId,
            Category = Categories.Order,
            LogCode = LogCodes.AgvSentActionStartedForOrder,
            LogLevel = nameof(LogLevel.Information),
            Id = Guid.Empty,
            Variables = new Dictionary<string, string>
            {
                { LogLabels.OriginalActionId, context.Saga.CorrelationId.ToString() },
                { LogLabels.ActionId, context.Saga.CurrentFleetManagerActionId.ToString() }
            }
        };
        await context.Publish(new LogOccurred(logEntry, [Categories.Order]));
    }

    private async Task PublishOrderFinishedEvent(BehaviorContext<OrderExecutionSaga> context)
    {
        if (!context.Saga.IsFinished)
        {
            return;
        }
        await context.Publish(new ActionCompletedByAgv(context.Saga.CurrentFleetManagerActionId, context.Saga.Order.AssignedAgv!.CorrelationId));
        var logEntry = new LogEntryDto
        {
            ActionGroupCorrelationId = context.Saga.CorrelationId,
            AgvCorrelationId = context.Saga.Order.AssignedAgv!.CorrelationId,
            Category = Categories.Order,
            LogCode = LogCodes.AgvSentActionCompletedForOrder,
            LogLevel = nameof(LogLevel.Information),
            Id = Guid.Empty,
            Variables = new Dictionary<string, string>
            {
                { LogLabels.OriginalActionId, context.Saga.CorrelationId.ToString() },
                { LogLabels.ActionId, context.Saga.CurrentFleetManagerActionId.ToString() }
            }
        };
        await context.Publish(new LogOccurred(logEntry, [Categories.Order]));
    }

    private async Task PublishOrderFailedEvent(BehaviorContext<OrderExecutionSaga> context)
    {
        await context.Publish(new ActionFailedByAgv(context.Saga.CurrentFleetManagerActionId, context.Saga.Order.AssignedAgv!.CorrelationId));
        var logEntry = new LogEntryDto
        {
            ActionGroupCorrelationId = context.Saga.CorrelationId,
            AgvCorrelationId = context.Saga.Order.AssignedAgv!.CorrelationId,
            Category = Categories.Order,
            LogCode = LogCodes.AgvSentActionFailedForOrder,
            LogLevel = nameof(LogLevel.Information),
            Id = Guid.Empty,
            Variables = new Dictionary<string, string>
            {
                { LogLabels.OriginalActionId, context.Saga.CorrelationId.ToString() },
                { LogLabels.ActionId, context.Saga.CurrentFleetManagerActionId.ToString() },
                { LogLabels.Reason, context.Saga.ErrorReference ?? string.Empty }
            }
        };

        await context.Publish(new LogOccurred(logEntry, [Categories.Order]));
    }

    private async Task PublishOrderCanceledEvent(BehaviorContext<OrderExecutionSaga> context)
    {
        await context.Publish(new ActionAbortedByAgv(context.Saga.CurrentFleetManagerActionId, context.Saga.Order.AssignedAgv!.CorrelationId));
        var logEntry = new LogEntryDto
        {
            ActionGroupCorrelationId = context.Saga.CorrelationId,
            AgvCorrelationId = context.Saga.Order.AssignedAgv!.CorrelationId,
            Category = Categories.Order,
            LogCode = LogCodes.AgvSentActionAbortedForOrder,
            LogLevel = nameof(LogLevel.Information),
            Id = Guid.Empty,
            Variables = new Dictionary<string, string>
            {
                { LogLabels.OriginalActionId, context.Saga.CorrelationId.ToString() },
                { LogLabels.ActionId, context.Saga.CurrentFleetManagerActionId.ToString() }
            }
        };

        await context.Publish(new LogOccurred(logEntry, [Categories.Order]));
    }

    private async Task PublishOrderCancelFailedEvent(BehaviorContext<OrderExecutionSaga> context)
    {
        await context.Publish(new ActionAbortionFailedByAgv(context.Saga.CurrentFleetManagerActionId, context.Saga.Order.AssignedAgv!.CorrelationId));
        var logEntry = new LogEntryDto
        {
            ActionGroupCorrelationId = context.Saga.CorrelationId,
            AgvCorrelationId = context.Saga.Order.AssignedAgv!.CorrelationId,
            Category = Categories.Order,
            LogCode = LogCodes.AgvSentActionAbortionFailedForOrder,
            LogLevel = nameof(LogLevel.Information),
            Id = Guid.Empty,
            Variables = new Dictionary<string, string>
            {
                { LogLabels.OriginalActionId, context.Saga.CorrelationId.ToString() },
                { LogLabels.ActionId, context.Saga.CurrentFleetManagerActionId.ToString() },
                { LogLabels.Reason, context.Saga.ErrorReference ?? string.Empty }
            }
        };

        await context.Publish(new LogOccurred(logEntry, [Categories.Order]));
    }

    private async Task PublishNewBaseRequestedEvent(BehaviorContext<OrderExecutionSaga> context)
    {
        await context.Publish(new AgvNewBaseRequested(context.Saga.Order.AssignedAgv!.CorrelationId));
        context.Saga.NewBaseNotified = true;
    }
    #endregion
}