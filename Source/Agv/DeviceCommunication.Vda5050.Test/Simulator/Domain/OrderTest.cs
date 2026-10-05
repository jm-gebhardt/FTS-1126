using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain;

[TestClass]
public class OrderTest
{
    private static readonly DateTime _baseTime = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Creates an order whose base contains evenly-spaced released nodes.
    /// Each edge has <paramref name="edgeLength"/> units of length at <paramref name="speed"/> m/s,
    /// so EstimatedTravelTime = edgeLength / speed seconds.
    /// Nodes are spaced edgeLength units apart along the X axis starting at X = 0.
    /// </summary>
    private static Order CreatePreparedOrder(string[] nodeIds, double edgeLength = 10.0, float speed = 1.0f)
    {
        var nodes = nodeIds.Select((id, i) => new OrderNode
        {
            NodeId = id,
            SequenceId = i * 2,
            Released = true,
            NodePosition = new OrderNodePosition { X = i * edgeLength, Y = 0 }
        }).ToList();

        var edges = new List<OrderEdge>();
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            edges.Add(new OrderEdge
            {
                EdgeId = $"edge-{i}",
                SequenceId = i * 2 + 1,
                Released = true,
                StartNodeId = nodes[i].NodeId,
                EndNodeId = nodes[i + 1].NodeId,
                Length = edgeLength
            });
        }

        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            FullRoute = new Route(nodes, edges)
        };
        order.PrepareTravel(speed);
        return order;
    }

    [TestMethod]
    public void GetLastNode_NullLastNodeId_ReturnsFirstNode()
    {
        var order = CreatePreparedOrder(["A", "B", "C"]);
        order.StartTime = _baseTime;

        order.GetLastNode(null, _baseTime).NodeId.ShouldBe("A");
    }

    [TestMethod]
    public void GetLastNode_NullStartTime_ReturnsFirstNode()
    {
        var order = CreatePreparedOrder(["A", "B", "C"]);
        order.StartTime = null;

        order.GetLastNode("A", _baseTime).NodeId.ShouldBe("A");
    }

    [TestMethod]
    public void GetLastNode_TimeWithinFirstEdge_ReturnsFirstNode()
    {
        // Each edge takes 10 s; 5 s elapsed < 10 s → still on first segment
        var order = CreatePreparedOrder(["A", "B", "C"]);
        order.StartTime = _baseTime;

        order.GetLastNode("A", _baseTime.AddSeconds(5)).NodeId.ShouldBe("A");
    }

    [TestMethod]
    public void GetLastNode_TimeAfterFirstEdge_ReturnsSecondNode()
    {
        // 15 s elapsed > 10 s (first edge) → past first edge, still within second
        var order = CreatePreparedOrder(["A", "B", "C"]);
        order.StartTime = _baseTime;

        order.GetLastNode("A", _baseTime.AddSeconds(15)).NodeId.ShouldBe("B");
    }

    [TestMethod]
    public void GetLastNode_TimeAfterAllEdges_ReturnsLastNode()
    {
        // 30 s elapsed > 20 s (total) → at last node
        var order = CreatePreparedOrder(["A", "B", "C"]);
        order.StartTime = _baseTime;

        order.GetLastNode("A", _baseTime.AddSeconds(30)).NodeId.ShouldBe("C");
    }

    [TestMethod]
    public void GetCurrentEdge_AtFirstNode_ReturnsFirstEdge()
    {
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;

        var edge = order.GetCurrentEdge(null, _baseTime);

        edge.ShouldNotBeNull();
        edge.StartNodeId.ShouldBe("A");
        edge.EndNodeId.ShouldBe("B");
    }

    [TestMethod]
    public void GetCurrentEdge_AfterAllEdges_ReturnsNull()
    {
        // 20 s elapsed > 10 s → at last node, no outgoing edge
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;

        order.GetCurrentEdge("A", _baseTime.AddSeconds(20)).ShouldBeNull();
    }

    [TestMethod]
    public void GetCurrentEdge_AfterFirstEdge_ReturnsSecondEdge()
    {
        // 15 s elapsed → past first edge (A→B), on second edge (B→C)
        var order = CreatePreparedOrder(["A", "B", "C"]);
        order.StartTime = _baseTime;

        var edge = order.GetCurrentEdge("A", _baseTime.AddSeconds(15));

        edge.ShouldNotBeNull();
        edge.StartNodeId.ShouldBe("B");
        edge.EndNodeId.ShouldBe("C");
    }

    [TestMethod]
    public void HasFinishedActionOfType_EmptyPendingActions_ReturnsFalse()
    {
        var order = new Order();

        order.HasFinishedActionOfType(Vda5050Specifications.ActionTypes.Pick).ShouldBeFalse();
    }

    [TestMethod]
    public void HasFinishedActionOfType_FinishedActionOfMatchingType_ReturnsTrue()
    {
        var order = new Order
        {
            PendingActionStates = [new OrderActionState { ActionId = "a1", ActionType = Vda5050Specifications.ActionTypes.Pick, ActionStatus = VehicleActionStatus.Finished }]
        };

        order.HasFinishedActionOfType(Vda5050Specifications.ActionTypes.Pick).ShouldBeTrue();
    }

    [TestMethod]
    public void HasFinishedActionOfType_RunningActionOfMatchingType_ReturnsFalse()
    {
        var order = new Order
        {
            PendingActionStates = [new OrderActionState { ActionId = "a1", ActionType = Vda5050Specifications.ActionTypes.Pick, ActionStatus = VehicleActionStatus.Running }]
        };

        order.HasFinishedActionOfType(Vda5050Specifications.ActionTypes.Pick).ShouldBeFalse();
    }

    [TestMethod]
    public void HasFinishedActionOfType_FinishedActionOfDifferentType_ReturnsFalse()
    {
        var order = new Order
        {
            PendingActionStates = [new OrderActionState { ActionId = "a1", ActionType = Vda5050Specifications.ActionTypes.Drop, ActionStatus = VehicleActionStatus.Finished }]
        };

        order.HasFinishedActionOfType(Vda5050Specifications.ActionTypes.Pick).ShouldBeFalse();
    }

    [TestMethod]
    public void GetActionOnNode_NodeNotInRoute_ReturnsNull()
    {
        var order = CreatePreparedOrder(["A", "B"]);

        order.GetActionOnNode("X", 0, Vda5050Specifications.ActionTypes.Pick).ShouldBeNull();
    }

    [TestMethod]
    public void GetActionOnNode_ActionTypeNotOnNode_ReturnsNull()
    {
        var order = CreatePreparedOrder(["A", "B"]);

        order.GetActionOnNode("A", 0, Vda5050Specifications.ActionTypes.Pick).ShouldBeNull();
    }

    [TestMethod]
    public void GetActionOnNode_MatchingNodeAndType_ReturnsAction()
    {
        var action = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var node = new OrderNode { NodeId = "A", SequenceId = 0, Released = true, Actions = [action] };
        var order = new Order { FullRoute = new Route([node], []) };

        var result = order.GetActionOnNode("A", 0, Vda5050Specifications.ActionTypes.Pick);

        result.ShouldNotBeNull();
        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Pick);
    }

    [TestMethod]
    public void PrepareTravel_SetsEstimatedTravelTimeOnEdges()
    {
        var nodes = new List<OrderNode>
        {
            new() { NodeId = "A", SequenceId = 0, Released = true, NodePosition = new OrderNodePosition {X = 0, Y = 0}},
            new() { NodeId = "B", SequenceId = 2, Released = true,NodePosition = new OrderNodePosition {X = 10, Y = 0} }
        };
        var edge = new OrderEdge { EdgeId = "e1", SequenceId = 1, Released = true, StartNodeId = "A", EndNodeId = "B"};
        var order = new Order { FullRoute = new Route(nodes, [edge]) };

        order.PrepareTravel(2.0f); // 10 m / 2 m/s = 5 s

        edge.EstimatedTravelTime.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void PrepareTravel_AccumulatesDurationFromAllEdges()
    {
        var nodes = new List<OrderNode>
        {
            new() { NodeId = "A", SequenceId = 0, Released = true, NodePosition = new OrderNodePosition {X = 0, Y = 0} },
            new() { NodeId = "B", SequenceId = 2, Released = true, NodePosition = new OrderNodePosition {X = 10, Y = 0} },
            new() { NodeId = "C", SequenceId = 4, Released = true, NodePosition = new OrderNodePosition {X = 30, Y = 0} }
        };
        var edges = new List<OrderEdge>
        {
            new() { EdgeId = "e1", SequenceId = 1, Released = true, StartNodeId = "A", EndNodeId = "B"},
            new() { EdgeId = "e2", SequenceId = 3, Released = true, StartNodeId = "B", EndNodeId = "C"}
        };
        var order = new Order { FullRoute = new Route(nodes, edges) };

        order.PrepareTravel(1.0f); // 10 s + 20 s = 30 s

        order.Duration.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [TestMethod]
    public void PrepareTravel_SetsRouteState()
    {
        var nodes = new List<OrderNode>
        {
            new() { NodeId = "A", SequenceId = 0, Released = true },
            new() { NodeId = "B", SequenceId = 2, Released = true }
        };
        var edge = new OrderEdge { EdgeId = "e1", SequenceId = 1, Released = true, StartNodeId = "A", EndNodeId = "B" };
        var order = new Order { FullRoute = new Route(nodes, [edge]) };
        order.CurrentRoute.Nodes.Count.ShouldBe(0);

        order.PrepareTravel(1.0f);

        order.CurrentRoute.Nodes.Count.ShouldBe(2);
        order.CurrentRoute.Edges.Count.ShouldBe(1);
    }

    [TestMethod]
    public void TryFinish_WhenNoActionsPending_ClearsStartTime()
    {
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;

        order.TryFinish(_baseTime.AddSeconds(20));

        order.StartTime.ShouldBeNull();
    }

    [TestMethod]
    public void TryFinish_WhenActionStillPending_PreservesStartTime()
    {
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;
        order.PendingActionStates.Add(new OrderActionState
        {
            ActionId = "a1",
            ActionType = Vda5050Specifications.ActionTypes.Pick,
            ActionStatus = VehicleActionStatus.Running
        });

        order.TryFinish(_baseTime.AddSeconds(20));

        order.StartTime.ShouldBe(_baseTime);
    }

    [TestMethod]
    public void TryFinish_WhenCompletable_ClearsStatus()
    {
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;
        order.State = new OrderState { OrderId = "order-1" };

        order.TryFinish(_baseTime.AddSeconds(20)); // 20 s >= 10 s duration, no pending actions -> completable

        order.State.ShouldBeNull();
    }

    [TestMethod]
    public void TryFinish_WhenNotCompletable_PreservesStatus()
    {
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;
        order.State = new OrderState { OrderId = "order-1" };

        order.TryFinish(_baseTime.AddSeconds(5));

        order.State.ShouldNotBeNull();
    }

    [TestMethod]
    public void Proceed_SetsStatusWithCurrentNodeSequenceId()
    {
        var order = CreatePreparedOrder(["A", "B"]);
        order.OrderId = "order-1";
        order.StartTime = _baseTime;

        order.Proceed(null, _baseTime);

        order.State.ShouldNotBeNull();
        order.State!.LastNodeSequenceId.ShouldBe(0);
        order.State.OrderId.ShouldBe("order-1");
    }

    [TestMethod]
    public void Proceed_AtLastBaseNode_SetsNewBaseRequest()
    {
        // After traveling past the only edge (> 10 s), AGV is at last node
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;

        order.Proceed("A", _baseTime.AddSeconds(20));

        order.State!.NewBaseRequest?.ShouldBeTrue();
    }

    [TestMethod]
    public void Proceed_MidSecondEdge_ReturnsInterpolatedPositionOnSecondEdge()
    {
        var order = CreatePreparedOrder(["A", "B", "C"]);
        order.StartTime = _baseTime;

        var position = order.Proceed("A", _baseTime.AddSeconds(15));

        position.X.ShouldBe(15f);
    }

    [TestMethod]
    public void Proceed_DoesNotMutateFullRouteBase()
    {
        var order = CreatePreparedOrder(["A", "B", "C"]);
        order.StartTime = _baseTime;

        order.Proceed("A", _baseTime.AddSeconds(15));

        order.Base.Nodes.Count.ShouldBe(3);
        order.Base.Edges.Count.ShouldBe(2);
    }

    [TestMethod]
    public void Proceed_MidEdge_ReturnsInterpolatedPosition()
    {
        // A at X=0, B at X=10; edge takes 10 s. At t=5 s → halfway → X=5
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;

        var position = order.Proceed("A", _baseTime.AddSeconds(5));

        position.X.ShouldBe(5f);
        position.Y.ShouldBe(0f);
        position.PositionInitialized.ShouldBeTrue();
    }

    [TestMethod]
    public void Proceed_AtLastNode_ReturnsLastNodePosition()
    {
        // B is at X=10; at t=20 s the AGV has passed the edge (10 s) and is at B
        var order = CreatePreparedOrder(["A", "B"]);
        order.StartTime = _baseTime;

        var position = order.Proceed("A", _baseTime.AddSeconds(20));

        position.X.ShouldBe(10f);
        position.Y.ShouldBe(0f);
    }

    [TestMethod]
    public void Proceed_NodeWithWaitingAction_StartsAction()
    {
        var action = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var actionState = new OrderActionState { ActionId = action.ActionId, ActionType = action.ActionType, ActionStatus = VehicleActionStatus.Waiting };
        var node = new OrderNode { NodeId = "A", SequenceId = 0, Released = true, NodePosition = new OrderNodePosition { X = 0, Y = 0 }, Actions = [action] };
        var order = new Order
        {
            FullRoute = new Route([node], []),
            PendingActionStates = [actionState]
        };
        order.PrepareTravel(1.0f);
        order.StartTime = _baseTime;

        order.Proceed(null, _baseTime);

        actionState.ActionStatus.ShouldBe(VehicleActionStatus.Running);
    }

    [TestMethod]
    public void Proceed_RunningActionElapsed_MarksActionFinished()
    {
        // OrderAction.Duration is fixed at 10 s
        var action = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var actionState = new OrderActionState { ActionId = action.ActionId, ActionType = action.ActionType, ActionStatus = VehicleActionStatus.Waiting };
        var node = new OrderNode { NodeId = "A", SequenceId = 0, Released = true, NodePosition = new OrderNodePosition { X = 0, Y = 0 }, Actions = [action] };
        var order = new Order
        {
            FullRoute = new Route([node], []),
            PendingActionStates = [actionState]
        };
        order.PrepareTravel(1.0f);
        order.StartTime = _baseTime;

        order.Proceed(null, _baseTime); // starts action at BaseTime
        order.Proceed(null, _baseTime.AddSeconds(15)); // 15 s ≥ 10 s → finishes action

        actionState.ActionStatus.ShouldBe(VehicleActionStatus.Finished);
    }

    [TestMethod]
    public void Proceed_RunningActionNotElapsed_DoesNotFinishAction()
    {
        var action = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var actionState = new OrderActionState { ActionId = action.ActionId, ActionType = action.ActionType, ActionStatus = VehicleActionStatus.Waiting };
        var node = new OrderNode { NodeId = "A", SequenceId = 0, Released = true, NodePosition = new OrderNodePosition { X = 0, Y = 0 }, Actions = [action] };
        var order = new Order
        {
            FullRoute = new Route([node], []),
            PendingActionStates = [actionState]
        };
        order.PrepareTravel(1.0f);
        order.StartTime = _baseTime;

        order.Proceed(null, _baseTime); // starts action
        order.Proceed(null, _baseTime.AddSeconds(5)); // 5 s < 10 s → still running

        actionState.ActionStatus.ShouldBe(VehicleActionStatus.Running);
    }

    [TestMethod]
    public void Cancel_AddsCancelOrderActionStateAsFinished()
    {
        var order = new Order { PendingActionStates = [] };

        order.Cancel("cancel-1");

        var cancelState = order.PendingActionStates.Single(s => s.ActionType == Vda5050Specifications.ActionTypes.CancelOrder);
        cancelState.ActionId.ShouldBe("cancel-1");
        cancelState.ActionStatus.ShouldBe(VehicleActionStatus.Finished);
    }

    [TestMethod]
    public void Cancel_ClearsRouteState()
    {
        var order = CreatePreparedOrder(["A", "B"]);
        order.CurrentRoute.Nodes.Count.ShouldBeGreaterThan(0);

        order.Cancel("cancel-1");

        order.CurrentRoute.Nodes.Count.ShouldBe(0);
        order.CurrentRoute.Edges.Count.ShouldBe(0);
    }

    [TestMethod]
    public void Cancel_WithRunningAction_SetsActionToFailed()
    {
        var action = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var actionState = new OrderActionState { ActionId = action.ActionId, ActionType = action.ActionType, ActionStatus = VehicleActionStatus.Waiting };
        var node = new OrderNode { NodeId = "A", SequenceId = 0, Released = true, NodePosition = new OrderNodePosition { X = 0, Y = 0 }, Actions = [action] };
        var order = new Order
        {
            FullRoute = new Route([node], []),
            PendingActionStates = [actionState]
        };
        order.PrepareTravel(1.0f);
        order.StartTime = _baseTime;
        order.Proceed(null, _baseTime); // starts action → Running + sets CurrentAction
        actionState.ActionStatus.ShouldBe(VehicleActionStatus.Running);

        order.Cancel("cancel-1");

        actionState.ActionStatus.ShouldBe(VehicleActionStatus.Failed);
    }
}