using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Models;

[TestClass]
public class OrderTest
{
    [TestMethod]
    public void IsCompleted_NoStatus_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);

        order.IsCompleted().ShouldBeFalse();
    }

    [TestMethod]
    public void IsCompleted_NotAtLastNode_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false);

        order.IsCompleted().ShouldBeFalse();
    }

    [TestMethod]
    public void IsCompleted_AtLastNodeWithPendingAction_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        var lastNode = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last();
        var action = lastNode.Actions.First();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, lastNode.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Running }
        ]);

        order.IsCompleted().ShouldBeFalse();
    }

    [TestMethod]
    public void IsCompleted_AtLastNodeAndAllActionsFinished_ReturnsTrue()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        var lastNode = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last();
        var action = lastNode.Actions.First();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, lastNode.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Finished }
        ]);

        order.IsCompleted().ShouldBeTrue();
    }

    [TestMethod]
    public void TryUpdatePlannedRoute_CompletedOrder_ReturnsFailure()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        var lastNode = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last();
        var action = lastNode.Actions.First();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, order.OrderUpdateId, lastNode.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Finished }
        ]);
        var newRoute = RouteGenerator.GenerateFromNodeIds(["B", "C", "D"]);

        var result = order.TryUpdatePlannedRoute(newRoute);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(OrderErrors.OrderRouteUpdateOrderCompleted);
    }

    [TestMethod]
    public void TryUpdatePlannedRoute_StitchingNodeNotFound_ReturnsFailure()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5);
        var newRoute = RouteGenerator.GenerateFromNodeIds(["X", "Y", "Z"]);

        var result = order.TryUpdatePlannedRoute(newRoute);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(OrderErrors.OrderRouteUpdateStitchingNodeNotFound);
    }

    [TestMethod]
    public void TryUpdatePlannedRoute_ValidRoute_ReturnsSuccess()
    {
        var order = OrderGenerator.GeneratePickOrder("APL1", "map/NODE-10", 5);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, order.OrderUpdateId, 2, false); // AGV at node "1"@2 -> stitching node
        var newRoute = RouteGenerator.GenerateFromNodeIds(["1", "map/NODE-10", "map/NODE-11", "PS01"]);

        var result = order.TryUpdatePlannedRoute(newRoute);

        result.IsSuccess.ShouldBeTrue();
        // Nodes: APL1@0 (kept from original), new route stitched in starting at stitching node "1"@2
        List<OrderNode> nodes = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).ToList();
        nodes.Count.ShouldBe(5);
        nodes[0].NodeId.ShouldBe("APL1");
        nodes[0].SequenceId.ShouldBe(0);
        nodes[1].NodeId.ShouldBe("1");
        nodes[1].SequenceId.ShouldBe(2);
        nodes[2].NodeId.ShouldBe("map/NODE-10");
        nodes[2].SequenceId.ShouldBe(4);
        nodes[3].NodeId.ShouldBe("map/NODE-11");
        nodes[3].SequenceId.ShouldBe(6);
        nodes[4].NodeId.ShouldBe("PS01");
        nodes[4].SequenceId.ShouldBe(8);
        // Edges: original APL1→1@1 (seq<3) is kept; new edges are shifted to seq=3,5,7
        List<OrderEdge> edges = order.PlannedRoute.Edges.OrderBy(e => e.SequenceId).ToList();
        edges.Count.ShouldBe(4);
        edges[0].SequenceId.ShouldBe(1);
        edges[0].StartNodeId.ShouldBe("APL1");
        edges[0].EndNodeId.ShouldBe("1");
        edges[1].SequenceId.ShouldBe(3);
        edges[1].StartNodeId.ShouldBe("1");
        edges[1].EndNodeId.ShouldBe("map/NODE-10");
        edges[2].SequenceId.ShouldBe(5);
        edges[2].StartNodeId.ShouldBe("map/NODE-10");
        edges[2].EndNodeId.ShouldBe("map/NODE-11");
        edges[3].SequenceId.ShouldBe(7);
        edges[3].StartNodeId.ShouldBe("map/NODE-11");
        edges[3].EndNodeId.ShouldBe("PS01");
    }

    [TestMethod]
    public void TryUpdatePlannedRoute_NewRouteHasUnreleasedHorizonNodes_KeepsThemUnreleased()
    {
        var order = OrderGenerator.GeneratePickOrder("APL1", "map/NODE-10", 5);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, order.OrderUpdateId, 2, false); // AGV at node "1"@2 -> stitching node
        var newRoute = RouteGenerator.GenerateFromNodeIds(["1", "map/NODE-10", "map/NODE-11", "PS01"]);
        // Only "1" -> "map/NODE-10" has actually been released by traffic management; the rest is horizon.
        newRoute.Nodes.Single(n => n.NodeId == "map/NODE-10").Released = true;
        newRoute.Edges.Single(e => e.EndNodeId == "map/NODE-10").Released = true;

        var result = order.TryUpdatePlannedRoute(newRoute);

        result.IsSuccess.ShouldBeTrue();
        order.Route.Nodes.Single(n => n.NodeId == "map/NODE-11").Released.ShouldBeFalse();
        order.Route.Nodes.Single(n => n.NodeId == "PS01").Released.ShouldBeFalse();
        order.Route.Edges.Single(e => e.EndNodeId == "map/NODE-11").Released.ShouldBeFalse();
        order.Route.Edges.Single(e => e.EndNodeId == "PS01").Released.ShouldBeFalse();
        order.Route.Nodes.Single(n => n.NodeId == "map/NODE-10").Released.ShouldBeTrue();
    }

    [TestMethod]
    public void UpdateState_Initialized_TransitionsToCreated()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State.ShouldBe(State.Initialized);

        order.UpdateState();

        order.State.ShouldBe(State.Created);
    }

    [TestMethod]
    public void UpdateState_Created_WithStatusAndNoErrors_TransitionsToRunning()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Created;
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false);

        order.UpdateState();

        order.State.ShouldBe(State.Running);
    }

    [TestMethod]
    public void UpdateState_AnyState_WithErrors_TransitionsToFailed()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Running;
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.OrderError);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, errors: [error]);

        order.UpdateState();

        order.State.ShouldBe(State.Failed);
    }

    [TestMethod]
    public void UpdateState_Running_WhenCompleted_TransitionsToFinished()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Running;
        var lastNode = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last();
        var action = lastNode.Actions.First();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, lastNode.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Finished }
        ]);

        order.UpdateState();

        order.State.ShouldBe(State.Finished);
    }

    [TestMethod]
    public void ValidationFailed_CreatedWithValidationError_ReturnsTrue()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Failed;
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.ValidationError);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, errors: [error]);

        order.ValidationFailed.ShouldBeTrue();
    }

    [TestMethod]
    public void ValidationFailed_NotInCreatedState_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Running;
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.ValidationError);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, errors: [error]);

        order.ValidationFailed.ShouldBeFalse();
    }

    [TestMethod]
    public void UpdateFailed_RunningWithUpdateError_ReturnsTrue()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Failed;
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.OrderUpdateError);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, errors: [error]);

        order.UpdateFailed.ShouldBeTrue();
    }

    [TestMethod]
    public void UpdateFailed_NotRunning_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Created;
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.OrderUpdateError);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, errors: [error]);

        order.UpdateFailed.ShouldBeFalse();
    }

    [TestMethod]
    public void Failed_RunningWithOrderError_ReturnsTrue()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Failed;
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.OrderError);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, errors: [error]);

        order.Failed.ShouldBeTrue();
    }

    [TestMethod]
    public void Failed_NotRunning_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State = State.Created;
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.OrderError);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, errors: [error]);

        order.Failed.ShouldBeFalse();
    }

    [TestMethod]
    public void GetActionStateById_NoStatus_ReturnsNull()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);

        order.GetActionStateById("action-1").ShouldBeNull();
    }

    [TestMethod]
    public void GetActionStateById_KnownId_ReturnsActionState()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        var actionState = new OrderActionState { ActionId = "action-1", ActionType = Vda5050Specifications.ActionTypes.Pick, ActionStatus = VehicleActionStatus.Running };
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, [actionState]);

        var result = order.GetActionStateById("action-1");

        result.ShouldNotBeNull();
        result.ActionId.ShouldBe("action-1");
    }

    [TestMethod]
    public void GetActionStateByType_NoStatus_ReturnsNull()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);

        order.GetActionStateByType("pick").ShouldBeNull();
    }

    [TestMethod]
    public void GetActionStateByType_KnownType_ReturnsActionState()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        var actionState = new OrderActionState { ActionId = "action-1", ActionType = Vda5050Specifications.ActionTypes.Pick, ActionStatus = VehicleActionStatus.Running };
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, [actionState]);

        var result = order.GetActionStateByType(Vda5050Specifications.ActionTypes.Pick);

        result.ShouldNotBeNull();
        result.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Pick);
    }

    [TestMethod]
    public void Fail_SetsStateFailed()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.State.ShouldBe(State.Initialized);

        order.Fail();

        order.State.ShouldBe(State.Failed);
    }

    [TestMethod]
    public void Reset_ResetsToInitialState()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 2, false);
        order.State = State.Running;
        order.PreviousState = State.Created;

        order.Reset();

        order.OrderUpdateId.ShouldBe(0);
        order.Status.ShouldBeNull();
        order.State.ShouldBe(State.Initialized);
        order.PreviousState.ShouldBeNull();
        order.Route.IsEmpty.ShouldBeTrue();
    }
}