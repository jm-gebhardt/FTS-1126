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
    public void TryUpdate_ValidData_BaseAndHorizonSet()
    {
        const int horizonLength = 2;
        const int baseLength = 2;
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5);
        order.PlannedRoute.Nodes.Count.ShouldBe(5);
        order.TryUpdate(baseLength, horizonLength).ShouldBeTrue();

        order.Base.Nodes.Count.ShouldBe(baseLength);
        order.Horizon.Nodes.Count.ShouldBe(horizonLength);
        order.Route.Nodes.ShouldBe([..order.Base.Nodes, ..order.Horizon.Nodes]);
        order.Route.Edges.ShouldBe([..order.Base.Edges, ..order.Horizon.Edges]);
        order.Route.Edges.Count.ShouldBe(order.Route.Nodes.Count - 1);
        order.Base.Nodes.All(n => n.Released).ShouldBeTrue();
        order.Base.Edges.All(n => n.Released).ShouldBeTrue();
        order.Horizon.Edges.All(n => !n.Released).ShouldBeTrue();
        order.Horizon.Nodes.All(n => !n.Released).ShouldBeTrue();
    }

    [TestMethod]
    public void TryUpdate_ValidData_OrderUpdateIdsSet()
    {
        const int horizonLength = 2;
        const int baseLength = 2;
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5);
        order.OrderUpdateId.ShouldBe(0);

        // Initial setting of base and horizon
        order.TryUpdate(baseLength, horizonLength).ShouldBeTrue();
        order.OrderUpdateId.ShouldBe(0);

        // Increment for all further updates
        order.TryUpdate(baseLength, horizonLength).ShouldBeTrue();
        order.OrderUpdateId.ShouldBe(1);
    }

    [TestMethod]
    public void TryUpdate_NoPlannedRoute_ReturnsFalse()
    {
        const int horizonLength = 2;
        const int baseLength = 2;
        var order = new Order(){PlannedRoute = new Route([], [])};
        order.PlannedRoute.IsEmpty.ShouldBeTrue();

        order.TryUpdate(baseLength, horizonLength).ShouldBeFalse();
    }

    [TestMethod]
    public void TryUpdate_CompletedOrder_ReturnsFalseAndClearsRoute()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5);
        order.TryUpdate(2, 2);
        int lastNodeSeqId = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last().SequenceId;
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, order.OrderUpdateId, lastNodeSeqId, false);

        order.TryUpdate(2, 2).ShouldBeFalse();

        order.Route.IsEmpty.ShouldBeTrue();
    }

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
    public void TryUpdatePlannedRoute_CompletedOrder_ReturnsSuccess()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.TryUpdate(2, 2);
        var lastNode = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last();
        var action = lastNode.Actions.First();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, order.OrderUpdateId, lastNode.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Finished }
        ]);
        var newRoute = RouteGenerator.GenerateFromNodeIds(["B", "C", "D"]);

        var result = order.TryUpdatePlannedRoute(newRoute, 2, 2);

        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
    }

    [TestMethod]
    public void TryUpdatePlannedRoute_StitchingNodeNotFound_ReturnsFailure()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5);
        order.TryUpdate(2, 2);
        var newRoute = RouteGenerator.GenerateFromNodeIds(["X", "Y", "Z"]);

        var result = order.TryUpdatePlannedRoute(newRoute, 2, 2);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(OrderErrors.OrderRouteUpdateStitchingNodeNotFound);
    }

    [TestMethod]
    public void TryUpdatePlannedRoute_ValidRoute_ReturnsSuccess()
    {
        var order = OrderGenerator.GeneratePickOrder("APL1", "map/NODE-10", 5);
        order.TryUpdate(2, 2);
        // After TryUpdate, base last node is "1" (seq=2)
        var newRoute = RouteGenerator.GenerateFromNodeIds(["1", "map/NODE-10", "map/NODE-11", "PS01"]);

        var result = order.TryUpdatePlannedRoute(newRoute, 2, 2);

        result.IsSuccess.ShouldBeTrue();
        // Nodes: APL1@0 (kept from original), new route stitched in starting at stitching node "1"@2
        List<OrderNode> nodes = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).ToList();
        nodes.Count.ShouldBe(5);
        nodes[0].NodeId.ShouldBe("APL1");  nodes[0].SequenceId.ShouldBe(0);
        nodes[1].NodeId.ShouldBe("1");            nodes[1].SequenceId.ShouldBe(2);
        nodes[2].NodeId.ShouldBe("map/NODE-10");            nodes[2].SequenceId.ShouldBe(4);
        nodes[3].NodeId.ShouldBe("map/NODE-11");            nodes[3].SequenceId.ShouldBe(6);
        nodes[4].NodeId.ShouldBe("PS01");            nodes[4].SequenceId.ShouldBe(8);
        // Edges: original APL1→1@1 (seq<3) is kept; new edges are shifted to seq=3,5,7
        List<OrderEdge> edges = order.PlannedRoute.Edges.OrderBy(e => e.SequenceId).ToList();
        edges.Count.ShouldBe(4);
        edges[0].SequenceId.ShouldBe(1); edges[0].StartNodeId.ShouldBe("APL1"); edges[0].EndNodeId.ShouldBe("1");
        edges[1].SequenceId.ShouldBe(3); edges[1].StartNodeId.ShouldBe("1");           edges[1].EndNodeId.ShouldBe("map/NODE-10");
        edges[2].SequenceId.ShouldBe(5); edges[2].StartNodeId.ShouldBe("map/NODE-10");           edges[2].EndNodeId.ShouldBe("map/NODE-11");
        edges[3].SequenceId.ShouldBe(7); edges[3].StartNodeId.ShouldBe("map/NODE-11");           edges[3].EndNodeId.ShouldBe("PS01");
    }

    [TestMethod]
    public void TryUpdatePlannedRoute_NewBaseRequestPending_TriggersTryUpdate()
    {
        var order = OrderGenerator.GeneratePickOrder("APL1", "map/NODE-10", 5);
        order.TryUpdate(2, 2);
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, order.OrderUpdateId, 2, true);
        var newRoute = RouteGenerator.GenerateFromNodeIds(["1", "map/NODE-10", "map/NODE-11", "PS01"]);

        order.TryUpdatePlannedRoute(newRoute, 2, 2);

        // TryUpdate was triggered by NewBaseRequest, incrementing the OrderUpdateId
        order.OrderUpdateId.ShouldBe(1);
        // PlannedRoute was updated: map/NODE-10@0 kept, new nodes/edges stitched in at stitching node "1"@2
        List<OrderNode> nodes = order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).ToList();
        nodes.Count.ShouldBe(5);
        nodes[0].NodeId.ShouldBe("APL1"); nodes[0].SequenceId.ShouldBe(0);
        nodes[1].NodeId.ShouldBe("1"); nodes[1].SequenceId.ShouldBe(2);
        nodes[2].NodeId.ShouldBe("map/NODE-10"); nodes[2].SequenceId.ShouldBe(4);
        nodes[3].NodeId.ShouldBe("map/NODE-11"); nodes[3].SequenceId.ShouldBe(6);
        nodes[4].NodeId.ShouldBe("PS01"); nodes[4].SequenceId.ShouldBe(8);
        // Edges: original APL1→1@1 (seq<3) is kept; new edges are shifted to seq=3,5,7
        List<OrderEdge> edges = order.PlannedRoute.Edges.OrderBy(e => e.SequenceId).ToList();
        edges.Count.ShouldBe(4);
        edges[0].SequenceId.ShouldBe(1); edges[0].StartNodeId.ShouldBe("APL1"); edges[0].EndNodeId.ShouldBe("1");
        edges[1].SequenceId.ShouldBe(3); edges[1].StartNodeId.ShouldBe("1"); edges[1].EndNodeId.ShouldBe("map/NODE-10");
        edges[2].SequenceId.ShouldBe(5); edges[2].StartNodeId.ShouldBe("map/NODE-10"); edges[2].EndNodeId.ShouldBe("map/NODE-11");
        edges[3].SequenceId.ShouldBe(7); edges[3].StartNodeId.ShouldBe("map/NODE-11"); edges[3].EndNodeId.ShouldBe("PS01");
    }

    [TestMethod]
    public void IsRunningActionOnNode_NoStatus_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.TryUpdate(2, 2);
        var lastNode = order.Route.Nodes.OrderBy(n => n.SequenceId).Last();

        order.IsRunningActionOnNode(lastNode.SequenceId).ShouldBeFalse();
    }

    [TestMethod]
    public void IsRunningActionOnNode_EmptyActionStates_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.TryUpdate(2, 2);
        var lastNode = order.Route.Nodes.OrderBy(n => n.SequenceId).Last();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, lastNode.SequenceId, false, []);

        order.IsRunningActionOnNode(lastNode.SequenceId).ShouldBeFalse();
    }

    [TestMethod]
    public void IsRunningActionOnNode_RunningAction_ReturnsTrue()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.TryUpdate(2, 2);
        var nodeWithAction = order.Route.Nodes.Single(n => n.NodeId == "PS01");
        var action = nodeWithAction.Actions.First();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, nodeWithAction.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Running }
        ]);

        order.IsRunningActionOnNode(nodeWithAction.SequenceId).ShouldBeTrue();
    }

    [TestMethod]
    public void IsRunningActionOnNode_FinishedAction_ReturnsFalse()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.TryUpdate(2, 2);
        var nodeWithAction = order.Route.Nodes.Single(n => n.NodeId == "PS01");
        var action = nodeWithAction.Actions.First();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, nodeWithAction.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Finished }
        ]);

        order.IsRunningActionOnNode(nodeWithAction.SequenceId).ShouldBeFalse();
    }

    [TestMethod]
    public void TryProceed_EmptyBase_ReturnsFailure()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.Base.IsEmpty.ShouldBeTrue();

        var result = order.TryProceed();

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(OrderErrors.ProceedBaseEmpty);
    }

    [TestMethod]
    public void TryProceed_NoStatus_ReturnsFailure()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.TryUpdate(2, 2);
        order.Status.ShouldBeNull();

        var result = order.TryProceed();

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(OrderErrors.ProceedNoLastNode);
    }

    [TestMethod]
    public void TryProceed_RunningActionOnCurrentNode_ReturnsSuccessWithoutProceeding()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.TryUpdate(2, 2);
        var nodeWithAction = order.Route.Nodes.Single(n => n.NodeId == "PS01");
        var action = nodeWithAction.Actions.First();
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, nodeWithAction.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Running }
        ]);
        int nodeCountBefore = order.Route.Nodes.Count;

        var result = order.TryProceed();

        result.IsSuccess.ShouldBeTrue();
        order.Route.Nodes.Count.ShouldBe(nodeCountBefore);
    }

    [TestMethod]
    public void TryProceed_NoRunningAction_SucceedsAndReducesRoute()
    {
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 3);
        order.TryUpdate(2, 2);
        // Use node "1" at seq=2 which has no actions
        order.Status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 2, false, []);
        int nodeCountBefore = order.Route.Nodes.Count;

        var result = order.TryProceed();

        result.IsSuccess.ShouldBeTrue();
        order.Route.Nodes.Count.ShouldBe(nodeCountBefore - 1);
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
        order.TryUpdate(2, 2);
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
