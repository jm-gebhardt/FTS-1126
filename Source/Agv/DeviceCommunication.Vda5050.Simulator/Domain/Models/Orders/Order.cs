using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;

/// <summary>
/// An order for the agv.
/// </summary>
[PublicAPI]
public class Order
{
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
    /// The full route of the order.
    /// </summary>
    public Route FullRoute { get; set; } = new([], []);

    /// <summary>
    /// The current base.
    /// </summary>
    public Route Base => FullRoute.GetBase();

    /// <summary>
    /// The current route (excluding passed nodes and edges).
    /// </summary>

    public Route CurrentRoute { get; set; } = new([], []);

    /// <summary>
    /// Contains a list of the current actions and the actions which are yet to be finished.
    /// </summary>
    public IList<OrderActionState> PendingActionStates { get; set; } = [];

    /// <summary>
    /// The current action of the agv
    /// </summary>
    public OrderAction? CurrentAction { get; private set; }

    /// <summary>
    /// The most recent state of the order.
    /// </summary>
    public OrderState? State { get; set; }

    /// <summary>
    /// The start time of the order.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// The duration of the order.
    /// </summary>
    public TimeSpan Duration { get; private set; } = TimeSpan.FromSeconds(0);

    private double _totalPausedSeconds;

    private AgvPosition? _currentActionPosition;

    /// <summary>
    /// Get the current last node
    /// </summary>
    /// <param name="lastNodeId">Previous last node id.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The current node.</returns>
    public OrderNode GetLastNode(string? lastNodeId, DateTime now)
    {
        var sortedNodes = Base.Nodes.OrderBy(n => n.SequenceId).ToList();
        if (lastNodeId == null || StartTime == null)
        {
            return sortedNodes.First();
        }
        double timePassed = (now - StartTime.Value).TotalSeconds - _totalPausedSeconds;
        if (CurrentAction != null)
        {
            timePassed -= (now - CurrentAction.StartedTime!.Value).TotalSeconds;
        }
        timePassed = Math.Max(0, timePassed);
        foreach (var edge in Base.Edges.OrderBy(e => e.SequenceId))
        {
            double edgeTime = edge.EstimatedTravelTime.TotalSeconds;
            if (timePassed < edgeTime)
            {
                return sortedNodes.First(n => n.SequenceId == edge.SequenceId - 1);
            }
            timePassed -= edgeTime;
        }
        return sortedNodes.Last();
    }

    /// <summary>
    /// Get the current edge.
    /// </summary>
    /// <param name="lastNodeId">Previous last node id.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The current edge.</returns>
    public OrderEdge? GetCurrentEdge(string? lastNodeId, DateTime now)
    {
        var currentNode = GetLastNode(lastNodeId, now);
        return Base.Edges.FirstOrDefault(e => e.SequenceId == currentNode.SequenceId + 1);
    }

    /// <summary>
    /// Proceed with the order.
    /// </summary>
    public AgvPosition Proceed(string? lastNodeId, DateTime now, ILogger? logger = null)
    {
        if (CurrentAction != null)
        {
            // startCharging starts the charging session. The action must keep running while the AGV charges and is
            // only finished externally (StopCharging instant action, battery full, or order abort)
            if (CurrentAction.ActionType == Vda5050Specifications.ActionTypes.StartCharging)
            {
                var current = GetLastNode(lastNodeId, now);
                return new AgvPosition { Theta = (float)(current.NodePosition?.Theta ?? 0), X = (float)(current.NodePosition?.X ?? 0), Y = (float)(current.NodePosition?.Y ?? 0), Z = 0 };
            }
            double timePassed = (now - CurrentAction.StartedTime!.Value).TotalSeconds;
            if (timePassed >= CurrentAction.Duration.TotalSeconds)
            {
                _totalPausedSeconds += CurrentAction.Duration.TotalSeconds;
                PendingActionStates.First(p => p.ActionId == CurrentAction.ActionId).ActionStatus = VehicleActionStatus.Finished;
                logger?.LogInformation("Order {OrderId}: action {ActionId} ({ActionType}) transitioned to Finished after {Elapsed:F1}s (duration {Duration:F1}s)",
                    OrderId, CurrentAction.ActionId, CurrentAction.ActionType, timePassed, CurrentAction.Duration.TotalSeconds);
                CurrentAction = null;
                _currentActionPosition = null;
            }
            else
            {
                if (_currentActionPosition != null)
                {
                    return _currentActionPosition;
                }
                var current = GetLastNode(lastNodeId, now);
                return new AgvPosition { Theta = (float)(current.NodePosition?.Theta ?? 0), X = (float)(current.NodePosition?.X ?? 0), Y = (float)(current.NodePosition?.Y ?? 0), Z = 0 };
            }
        }
        var currentNode = GetLastNode(lastNodeId, now);
        CurrentRoute.Proceed(currentNode.SequenceId);
        if (currentNode.NodeId != lastNodeId)
        {
        }
        State = new OrderState
        {
            Errors =
            [
            ],
            LastNodeSequenceId = currentNode.SequenceId,
            NewBaseRequest = Base.Nodes.OrderBy(n => n.SequenceId)
                                 .Last()
                                 .SequenceId ==
                             currentNode.SequenceId,
            OrderId = OrderId,
            OrderUpdateId = OrderUpdateId
        };
        if (currentNode.Actions.Count > 0)
        {
            var currentAction = PendingActionStates.FirstOrDefault(p => currentNode.ActionIds.Contains(p.ActionId) && p.ActionStatus == VehicleActionStatus.Waiting);
            if (currentAction != null)
            {
                currentAction.ActionStatus = VehicleActionStatus.Running;
                CurrentAction = currentNode.Actions.First(a => a.ActionId == currentAction.ActionId);
                CurrentAction.StartedTime = now;
                State!.ActionStates = PendingActionStates;
                _currentActionPosition = new AgvPosition { Theta = (float)(currentNode.NodePosition?.Theta ?? 0), X = (float)(currentNode.NodePosition?.X ?? 0), Y = (float)(currentNode.NodePosition?.Y ?? 0), Z = 0 };
                logger?.LogInformation("Order {OrderId}: action {ActionId} ({ActionType}) transitioned to Running at node {NodeId}",
                    OrderId, CurrentAction.ActionId, CurrentAction.ActionType, currentNode.NodeId);
                return _currentActionPosition;
            }
        }
        var currentEdge = GetCurrentEdge(lastNodeId, now);
        if (currentEdge == null)
        {
            return new AgvPosition { Theta = (float)(currentNode.NodePosition?.Theta ?? 0), X = (float)(currentNode.NodePosition?.X ?? 0), Y = (float)(currentNode.NodePosition?.Y ?? 0), Z = 0 };
        }
        if (CurrentAction == null && currentEdge.Actions.Count > 0)
        {
            var currentAction = PendingActionStates.FirstOrDefault(p => currentEdge.ActionIds.Contains(p.ActionId) && p.ActionStatus == VehicleActionStatus.Waiting);
            if (currentAction != null)
            {
                currentAction.ActionStatus = VehicleActionStatus.Running;
                CurrentAction = currentEdge.Actions.First(a => a.ActionId == currentAction.ActionId);
                CurrentAction.StartedTime = now;
                State!.ActionStates = PendingActionStates;
            }
        }
        if (currentEdge.EstimatedTravelTime.TotalSeconds == 0)
        {
            return new AgvPosition { Theta = (float)(currentNode.NodePosition?.Theta ?? 0), X = (float)(currentNode.NodePosition?.X ?? 0), Y = (float)(currentNode.NodePosition?.Y ?? 0), Z = 0 };
        }
        double secondsSpendOnCurrentEdge = (now - StartTime!.Value).TotalSeconds - _totalPausedSeconds
                                                                                 - Base.Edges.Where(e => e.SequenceId < currentEdge.SequenceId).Sum(e => e.EstimatedTravelTime.TotalSeconds);
        double travelledFraction = secondsSpendOnCurrentEdge / currentEdge.EstimatedTravelTime.TotalSeconds;
        AgvPosition position;
        if (currentEdge.Trajectory == null)
        {
            var endNode = Base.Nodes.FirstOrDefault(n => n.SequenceId == currentEdge.SequenceId + 1);
            double fraction = Math.Clamp(travelledFraction, 0, 1);
            float x = (float)((currentNode.NodePosition?.X ?? 0) + fraction * ((endNode?.NodePosition?.X ?? 0) - (currentNode.NodePosition?.X ?? 0)));
            float y = (float)((currentNode.NodePosition?.Y ?? 0) + fraction * ((endNode?.NodePosition?.Y ?? 0) - (currentNode.NodePosition?.Y ?? 0)));
            position = new AgvPosition { X = x, Y = y, Theta = (float)(currentNode.NodePosition?.Theta ?? 0), Z = 0, PositionInitialized = true };
        }
        else
        {
            var startNode = Base.Nodes.First(n => n.NodeId == currentEdge.StartNodeId);
            var endNode = Base.Nodes.First(n => n.NodeId == currentEdge.EndNodeId);
            position = currentEdge.GetPosition(travelledFraction, startNode, endNode);
        }
        if (CurrentAction != null)
        {
            _currentActionPosition = position;
        }
        return position;
    }

    /// <summary>
    /// True if a subsequent <see cref="TryFinish"/> call at the given time would mark the order as finished.
    /// </summary>
    public bool IsCompletable(DateTime now)
    {
        if (StartTime == null)
        {
            return false;
        }
        if (CurrentAction != null || PendingActionStates.Any(s => s.ActionStatus is VehicleActionStatus.Waiting or VehicleActionStatus.Running))
        {
            return false;
        }
        double netTravelTime = (now - StartTime.Value).TotalSeconds - _totalPausedSeconds;
        if (netTravelTime < Duration.TotalSeconds)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Try finish.
    /// </summary>
    public void TryFinish(DateTime now)
    {
        if (!IsCompletable(now))
        {
            return;
        }
        State = null;
        StartTime = null;
    }

    /// <summary>
    /// Finish a running <c>startCharging</c> action explicitly. Used when the AGV is told to stop charging
    /// (StopCharging instant action) or when the battery is fully charged. Returns true if an action was finished.
    /// </summary>
    public bool FinishCurrentChargingAction(DateTime now, ILogger? logger = null)
    {
        if (CurrentAction is not { ActionType: Vda5050Specifications.ActionTypes.StartCharging })
        {
            return false;
        }
        var actionToFinish = CurrentAction;
        double elapsed = (now - actionToFinish.StartedTime!.Value).TotalSeconds;
        _totalPausedSeconds += elapsed;
        PendingActionStates.First(p => p.ActionId == actionToFinish.ActionId).ActionStatus = VehicleActionStatus.Finished;
        logger?.LogInformation("Order {OrderId}: action {ActionId} ({ActionType}) transitioned to Finished after {Elapsed:F1}s (charging ended)",
            OrderId, actionToFinish.ActionId, actionToFinish.ActionType, elapsed);
        CurrentAction = null;
        return true;
    }

    /// <summary>
    /// Prepare order for agv travel.
    /// </summary>
    public void PrepareTravel(float agvSpeed, ILogger? logger = null)
    {
        var durationBefore = Duration;
        foreach (var edge in Base.Edges)
        {
            var startNode = Base.Nodes.First(n => n.NodeId == edge.StartNodeId);
            var endNode = Base.Nodes.First(n => n.NodeId == edge.EndNodeId);
            edge.Prepare(agvSpeed, startNode, endNode);
            Duration += edge.EstimatedTravelTime;
        }
        // Copy full route, as the current route shrinks
        CurrentRoute = new Route([..FullRoute.Nodes], [..FullRoute.Edges]);
        logger?.LogInformation("PrepareTravel[{OrderId} u{UpdateId}] edges={EdgeCount} durationBefore={Before:F2}s durationAfter={After:F2}s perEdge=[{Edges}]",
            OrderId, OrderUpdateId, Base.Edges.Count, durationBefore.TotalSeconds, Duration.TotalSeconds,
            string.Join(",", Base.Edges.OrderBy(e => e.SequenceId).Select(e => $"{e.SequenceId}:{e.EstimatedTravelTime.TotalSeconds:F2}s")));
    }

    /// <summary>
    /// Carry travel progress from a previous order so an order update continues
    /// from the AGV's current position instead of restarting at the route start.
    /// </summary>
    /// <param name="previous">The previous order the AGV was executing.</param>
    public void CarryOverProgressFrom(Order previous)
    {
        StartTime = previous.StartTime;
        _totalPausedSeconds = previous._totalPausedSeconds;
        CurrentAction = previous.CurrentAction;
        _currentActionPosition = previous._currentActionPosition;
        foreach (var previousState in previous.PendingActionStates)
        {
            var carried = PendingActionStates.FirstOrDefault(s => s.ActionId == previousState.ActionId);
            if (carried != null)
            {
                carried.ActionStatus = previousState.ActionStatus;
            }
        }
    }

    /// <summary>
    /// True, if the order finished an action of the given type.
    /// </summary>
    /// <param name="actionType">Type of the action.</param>
    public bool HasFinishedActionOfType(string actionType)
    {
        return PendingActionStates.Any(a => a.ActionType == actionType && a.ActionStatus == VehicleActionStatus.Finished);
    }

    /// <summary>
    /// Get action of given type on the given node visit, identified by <paramref name="nodeId"/> and <paramref name="sequenceId"/>.
    /// </summary>
    public OrderAction? GetActionOnNode(string nodeId, int sequenceId, string actionType)
    {
        return FullRoute.Nodes.FirstOrDefault(n => n.NodeId == nodeId && n.SequenceId == sequenceId)?.Actions.FirstOrDefault(a => a.ActionType == actionType);
    }

    /// <summary>
    /// Cancel the current order.
    /// </summary>
    /// <param name="actionId">The cancelOrder action id.</param>
    public void Cancel(string actionId)
    {
        var currentState = PendingActionStates.SingleOrDefault(a => a.ActionType == CurrentAction?.ActionType);
        currentState?.ActionStatus = VehicleActionStatus.Failed;
        PendingActionStates.Add(new OrderActionState { ActionId = actionId, ActionType = Vda5050Specifications.ActionTypes.CancelOrder, ActionStatus = VehicleActionStatus.Finished });
        CurrentRoute = new Route([], []);
    }
}