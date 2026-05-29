using Gebhardt.Acc.Common.Domain.Models.Errors;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// An order for the agv.
/// </summary>
[PublicAPI]
public class Order
{
    /// <summary>
    /// The agv assigned to the order.
    /// </summary>
    public Agv? AssignedAgv { get; set; }

    /// <summary>
    /// Order identification. Used to identify multiple order messages that belong to the same order.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Order update identification. Is unique per orderId.
    /// If an order update is rejected, this field is to be passed in the rejection message.
    /// </summary>
    public int OrderUpdateId { get; set; }

    /// <summary>
    /// The planned route of the order.
    /// </summary>
    public Route PlannedRoute { get; set; } = null!;

    /// <summary>
    /// The current route.
    /// </summary>
    public Route Route { get; set; } = new([], []);

    /// <summary>
    /// Order validation by the vehicle failed.
    /// </summary>
    public bool ValidationFailed => State == State.Failed && Status?.ValidationError != null;

    /// <summary>
    /// Order update by the vehicle failed.
    /// </summary>
    public bool UpdateFailed => State == State.Failed && Status?.UpdateError != null;

    /// <summary>
    /// Order was reported failed by the vehicle.
    /// </summary>
    public bool Failed => State == State.Failed && Status?.OrderError != null;

    /// <summary>
    /// The most recent status of the order.
    /// </summary>
    public OrderState? Status { get; set; }

    /// <summary>
    /// The state of the order.
    /// </summary>
    public State State { get; set; } = State.Initialized;

    /// <summary>
    /// The previous state of the order.
    /// </summary>
    public State? PreviousState { get; set; }

    /// <summary>
    /// True, if the order is completed.
    /// </summary>
    public bool IsCompleted()
    {
        if (Status == null)
        {
            return false;
        }
        bool isAtTargetNode = Status != null && Status.LastNodeSequenceId == PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last().SequenceId;
        bool allActionsFinished = Status!.ActionStates.Count == 0 || Status!.ActionStates.All(a => a.ActionStatus == VehicleActionStatus.Finished);
        return isAtTargetNode && allActionsFinished;
    }

    /// <summary>
    /// Try updating the current planned route to the new one.
    /// </summary>
    /// <param name="newRoute">The new route.</param>
    /// <returns>Success, if the route was updated, Failure with a dedicated error, if the update failed.</returns>
    public Result TryUpdatePlannedRoute(Route newRoute)
    {
        if (IsCompleted())
        {
            return Result.Failure(OrderErrors.OrderRouteUpdateOrderCompleted);
        }
        var currentStitchingNode = (Status?.LastNodeSequenceId is { } seqId
                                       ? PlannedRoute.Nodes.SingleOrDefault(n => n.SequenceId == seqId)
                                       : null)
                                   ?? PlannedRoute.Nodes.OrderBy(n => n.SequenceId).First();
        var newStitchingNode = newRoute.Nodes.SingleOrDefault(n => n.NodeId == currentStitchingNode.NodeId);
        if (newStitchingNode == null)
        {
            return Result.Failure(OrderErrors.OrderRouteUpdateStitchingNodeNotFound);
        }
        newStitchingNode.Actions = currentStitchingNode.Actions; // Actions can not be changed on the stitching node.
        newRoute = newRoute.RemoveBeforeNode(newStitchingNode);
        newRoute.ShiftSequenceIds(currentStitchingNode.SequenceId);
        PlannedRoute = PlannedRoute.Replace(newRoute);
        Route = new Route(PlannedRoute.Nodes.ToList(), PlannedRoute.Edges.ToList());
        Route.Release();
        OrderUpdateId++;
        return Result.Success();
    }

    /// <summary>
    /// Update order state.
    /// </summary>
    public void UpdateState()
    {
        if (State == State.Initialized && Status == null)
        {
            State = State.Created;
            PreviousState = State.Initialized;
            return;
        }
        if (State == State.Created && Status != null && Status.Errors.IsNullOrEmpty())
        {
            State = State.Running;
            PreviousState = State.Created;
            return;
        }
        if (Status is { Errors.Count: > 0 })
        {
            State = State.Failed;
            return;
        }
        if (State == State.Running && IsCompleted())
        {
            State = State.Finished;
            PreviousState = State.Running;
            return;
        }
        PreviousState = null; // only set during state change
    }

    /// <summary>
    /// Get the state of the given action by its id.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    public OrderActionState? GetActionStateById(string actionId)
    {
        return Status?.ActionStates.SingleOrDefault(s => s.ActionId == actionId);
    }

    /// <summary>
    /// Get the state of the given action by its type.
    /// </summary>
    /// <param name="actionType">The id of the action.</param>
    public OrderActionState? GetActionStateByType(string actionType)
    {
        return Status?.ActionStates.SingleOrDefault(s => s.ActionType == actionType);
    }

    /// <summary>
    /// Fail the order.
    /// </summary>
    public void Fail()
    {
        State = State.Failed;
    }

    /// <summary>
    /// Reset the order to initial state.
    /// </summary>
    public void Reset()
    {
        OrderUpdateId = 0;
        Status = null;
        State = State.Initialized;
        PreviousState = null;
        Route = new Route([], []);
    }
}

/// <summary>
/// The state of the order.
/// </summary>
public enum State
{
    /// <summary/>
    Initialized,

    /// <summary/>
    Created,

    /// <summary/>
    Running,

    /// <summary/>
    Failed,

    /// <summary/>
    Finished
}