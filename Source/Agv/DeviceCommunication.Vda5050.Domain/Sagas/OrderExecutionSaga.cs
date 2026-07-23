using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.HeadIds;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using JetBrains.Annotations;
using MassTransit;
using State = MassTransit.State;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Sagas;

/// <summary>
/// Provides a saga for order execution.
/// </summary>
[PublicAPI]
public class OrderExecutionSaga : SagaStateMachineInstance, CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the saga.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// The current action id used by the fleet manager.
    /// </summary>
    public Guid CurrentFleetManagerActionId { get; set; }

    /// <summary>
    /// The order to execute.
    /// </summary>
    public Order Order { get; set; }

    /// <summary>
    /// The order topic header id counter.
    /// </summary>
    public ISequenceCounter<IOrderHeaderId> OrderHeaderId { get; init; }

    /// <summary>
    /// The instant actions topic header id counter.
    /// </summary>
    public ISequenceCounter<IInstantActionsHeaderId> InstantActionsHeaderId { get; init; }

    /// <summary>
    /// The current state of the saga state machine.
    /// </summary>
    public State CurrentState { get; set; } = null!;

    /// <summary>
    /// Error reference, if smth went wrong.
    /// </summary>
    public string? ErrorReference { get; private set; }

    /// <summary>
    /// The action id of the instant action.
    /// </summary>
    public string? InstantActionId { get; private set; }

    /// <summary>
    /// True, if the initial order should be sent.
    /// </summary>
    public bool ShouldSendNextOrder => Order is { State: Models.Orders.State.Created, PreviousState: Models.Orders.State.Initialized };

    /// <summary>
    /// True, if the order was just started by the agv.
    /// </summary>
    public bool OrderStartedRunning => Order is { State: Models.Orders.State.Running, PreviousState: Models.Orders.State.Created };

    /// <summary>
    /// True, if the order was just completed by the agv.
    /// </summary>
    public bool IsFinished => Order.IsCompleted();

    /// <summary>
    /// True, if the AGV has requested a new base.
    /// </summary>
    public bool NewBaseRequested => Order.Status?.NewBaseRequest is true && !NewBaseNotified && !IsFinished;

    /// <summary>
    /// True, if the AGV new-base request has already been published.
    /// </summary>
    public bool NewBaseNotified { get; set; }

    /// <summary>
    /// True, if an update of the planned route has been requested.
    /// </summary>
    public bool RouteUpdateRequested { get; set; }

    /// <inheritdoc cref="OrderExecutionSaga"/>
    public OrderExecutionSaga(Guid correlationId, Order order, ISequenceCounter<IOrderHeaderId> headerId, ISequenceCounter<IInstantActionsHeaderId> instantActionsHeaderId)
    {
        CorrelationId = correlationId;
        Order = order;
        OrderHeaderId = headerId;
        InstantActionsHeaderId = instantActionsHeaderId;
        CurrentFleetManagerActionId = correlationId;
    }

    /// <summary>
    /// Initialize the order.
    /// </summary>
    public bool Initialize()
    {
        if (Order.PlannedRoute.IsEmpty)
        {
            Order.Fail();
            ErrorReference = OrderErrors.InitializationFailed.Description;
            return false;
        }
        Order.Route = new Route(Order.PlannedRoute.Nodes.ToList(), Order.PlannedRoute.Edges.ToList());
        Order.OrderUpdateId = 0;
        Order.UpdateState();
        return true;
    }

    /// <summary>
    /// Update the state of the order.
    /// </summary>
    /// <param name="state"></param>
    public bool TryUpdateState(OrderState state)
    {
        Order.Status = state;
        Order.UpdateState();
        if (TryExtractOrderErrors())
        {
            return false;
        }
        return true;
    }

    private bool TryExtractOrderErrors()
    {
        if (Order.ValidationFailed)
        {
            ErrorReference = string.Join(",", Order.Status!.ValidationError!.ErrorReferences?.Select(e => e.ToString()).ToList() ?? ["Order validation reported failed by agv w/o detailed error"]);
            return true;
        }
        if (Order.UpdateFailed)
        {
            ErrorReference = string.Join(",", Order.Status!.UpdateError!.ErrorReferences?.Select(e => e.ToString()).ToList() ?? ["Order update reported failed by agv w/o detailed error"]);
            return true;
        }
        if (Order.Failed)
        {
            ErrorReference = string.Join(",", Order.Status!.OrderError!.ErrorReferences?.Select(e => e.ToString()).ToList() ?? ["Order reported failed by agv w/o detailed error"]);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Prepare issuing an instant action
    /// </summary>
    public void PrepareInstantAction()
    {
        InstantActionId = Guid.NewGuid().ToString();
        InstantActionsHeaderId.Increase();
    }

    /// <summary>
    /// Reset the instant action.
    /// </summary>
    public void ResetInstantAction()
    {
        InstantActionId = null;
    }

    /// <summary>
    /// Try to update the order.
    /// </summary>
    /// <returns>True, if the order was updated.</returns>
    public bool TryUpdate(Order newOrder, Guid currentFleetManagerActionId)
    {
        CurrentFleetManagerActionId = currentFleetManagerActionId;
        RouteUpdateRequested = true; //prevent order updates until route update finished
        var result = Order.TryUpdatePlannedRoute(newOrder.PlannedRoute);
        if (!result.IsSuccess)
        {
            ErrorReference = result.Error!.Description;
            RouteUpdateRequested = false;
            return false;
        }
        RouteUpdateRequested = false;
        return true;
    }

    /// <summary>
    /// Restart the order.
    /// </summary>
    public bool Restart()
    {
        ErrorReference = null;
        RouteUpdateRequested = false;
        NewBaseNotified = false;
        InstantActionId = null;
        Order.Reset();
        return Initialize();
    }
}