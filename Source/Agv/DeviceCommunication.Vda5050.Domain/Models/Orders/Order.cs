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
    /// The current base.
    /// </summary>
    public Route Base => Route.GetBase();

    /// <summary>
    /// The current horizon.
    /// </summary>
    public Route Horizon => Route.GetHorizon();

    private bool IsInitialOrder => Base.Nodes.Count == 0 && OrderUpdateId == 0;

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
    /// Update the order, set new base and horizon.
    /// </summary>
    /// <param name="baseLength">The length of the base.</param>
    /// <param name="maxHorizonLength">The maximum horizon length.</param>
    /// <returns>True, if the order update was successful, false if there are no more planned steps in the route.</returns>
    public bool TryUpdate(int baseLength, int maxHorizonLength)
    {
        if (PlannedRoute.IsEmpty)
        {
            return false;
        }
        if (IsCompleted())
        {
            Route = new Route([], []);
            return false;
        }
        OrderUpdateId = IsInitialOrder ? -1 : OrderUpdateId;
        OrderUpdateId++;
        var stitchingNode = Base.Nodes.LastOrDefault() ?? PlannedRoute.Nodes.OrderBy(n => n.SequenceId).First();
        if (PlannedRoute.Nodes.Count == 1 && PlannedRoute.Edges.Count == 0)
        {
            Route = new Route([stitchingNode], []);
            Route.Release();
            return true;
        }
        if (stitchingNode.SequenceId == PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last().SequenceId)
        {
            Route = new Route([], []);
            return true;
        }
        var newBase = PlannedRoute.Subsample(stitchingNode.SequenceId, baseLength);
       
        newBase.Release();
        var horizonFirstNode = PlannedRoute.Nodes.SingleOrDefault(n => n.SequenceId == newBase.Nodes.Last().SequenceId + 2);
        if (horizonFirstNode == null)
        {
            Route = newBase;
            return true;
        }
        var newHorizon = PlannedRoute.Subsample(newBase.Nodes.Last().SequenceId, maxHorizonLength + 1);
        if (newHorizon.Edges.Count == 1 && newHorizon.Edges.Single().EdgeId == PlannedRoute.Edges.OrderBy(e => e.SequenceId).Last().EdgeId)
        {
            // If it is just one more edge, network latency will prevent any more updates,
            // release the rest of the route.
            newHorizon.Release();
        }
        Route = newBase.Stitch(newHorizon);
        return true;
    }

    /// <summary>
    /// Try updating the current planned route to the new one.
    /// </summary>
    /// <param name="newRoute">The new route.</param>
    /// <param name="baseLength">The length of the base.</param>
    /// <param name="maxHorizonLength">The maximum horizon length.</param>
    /// <returns>Success, if the route was updated, Failure with a dedicated error, if the update failed.</returns>
    public Result TryUpdatePlannedRoute(Route newRoute, int baseLength, int maxHorizonLength)
    {
        if (IsCompleted())
        {
            PlannedRoute = PlannedRoute.Replace(newRoute);
            TryUpdate(baseLength, maxHorizonLength);
            return Result.Success();
        }
        var currentStitchingNode = Base.Nodes.LastOrDefault() ?? PlannedRoute.Nodes.OrderBy(n => n.SequenceId).First();
        var newStitchingNode = newRoute.Nodes.SingleOrDefault(n => n.NodeId == currentStitchingNode.NodeId);
        if (newStitchingNode == null)
        {
            return Result.Failure(OrderErrors.OrderRouteUpdateStitchingNodeNotFound);
        }
        newStitchingNode.Actions = currentStitchingNode.Actions; // Actions can not be changed on the stitching node.
        newRoute = newRoute.RemoveBeforeNode(newStitchingNode);
        newRoute.ShiftSequenceIds(currentStitchingNode.SequenceId);
        PlannedRoute = PlannedRoute.Replace(newRoute);
        if (Status?.NewBaseRequest is true)
        {
            TryUpdate(baseLength, maxHorizonLength); // no need to handle failure, arrival was checked earlier
        }
        return Result.Success();
    }

    /// <summary>
    /// True, if the agv is currently running an action on the given node.
    /// </summary>
    /// <param name="nodeSequenceId">The sequence id of the node.</param>
    public bool IsRunningActionOnNode(int nodeSequenceId)
    {
        if (Status == null || Status.ActionStates.Count == 0)
        {
            return false;
        }
        var node = Route.Nodes.Single(n => n.SequenceId == nodeSequenceId);
        IEnumerable<string> nodeActions = node.Actions.Select(n => n.ActionId);
        return Status.ActionStates.Any(s => nodeActions.Contains(s.ActionId) && s.ActionStatus != VehicleActionStatus.Finished && s.ActionStatus != VehicleActionStatus.Failed);
    }

    /// <summary>
    /// Try to proceed the order.
    /// </summary>
    /// <returns>True, if the order could proceed.</returns>
    public Result TryProceed()
    {
        if (Base.IsEmpty)
        {
            return Result.Failure(OrderErrors.ProceedBaseEmpty);
        }
        if (Status?.LastNodeSequenceId == null)
        {
            return Result.Failure(OrderErrors.ProceedNoLastNode);
        }
        if (IsRunningActionOnNode(Status.LastNodeSequenceId))
        {
            // Do not proceed yet, agv is running action on node.
            return Result.Success();
        }
        return Route.Proceed(Status.LastNodeSequenceId);
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