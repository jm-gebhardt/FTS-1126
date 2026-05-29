using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.HeadIds;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Sagas;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain;

[TestClass]
public class OrderExecutionSagaTest
{
    private static OrderExecutionSaga CreateSaga(Order? order = null)
    {
        order ??= OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5);
        return new OrderExecutionSaga(
            Guid.NewGuid(),
            order,
            new SequenceCounter<IOrderHeaderId>(),
            new SequenceCounter<IInstantActionsHeaderId>()
        );
    }

    [TestMethod]
    public void Constructor_SetsCurrentFleetManagerActionIdToCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var saga = new OrderExecutionSaga(
            correlationId,
            OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5),
            new SequenceCounter<IOrderHeaderId>(),
            new SequenceCounter<IInstantActionsHeaderId>()
        );

        saga.CurrentFleetManagerActionId.ShouldBe(correlationId);
    }

    [TestMethod]
    public void Initialize_ValidOrder_ReturnsTrue()
    {
        var saga = CreateSaga();

        saga.Initialize().ShouldBeTrue();
    }

    [TestMethod]
    public void Initialize_ValidOrder_TransitionsOrderToCreated()
    {
        var saga = CreateSaga();

        saga.Initialize();

        saga.Order.State.ShouldBe(State.Created);
    }

    [TestMethod]
    public void Initialize_ValidOrder_SetsRoute()
    {
        var saga = CreateSaga();

        saga.Initialize();

        saga.Order.Route.IsEmpty.ShouldBeFalse();
    }

    [TestMethod]
    public void Initialize_EmptyRoute_ReturnsFalse()
    {
        var order = new Order { PlannedRoute = new Route([], []) };
        var saga = CreateSaga(order);

        saga.Initialize().ShouldBeFalse();
    }

    [TestMethod]
    public void Initialize_EmptyRoute_SetsErrorReference()
    {
        var order = new Order { PlannedRoute = new Route([], []) };
        var saga = CreateSaga(order);

        saga.Initialize();

        saga.ErrorReference.ShouldBe(OrderErrors.InitializationFailed.Description);
    }

    [TestMethod]
    public void Initialize_EmptyRoute_FailsOrder()
    {
        var order = new Order { PlannedRoute = new Route([], []) };
        var saga = CreateSaga(order);

        saga.Initialize();

        saga.Order.State.ShouldBe(State.Failed);
    }

    [TestMethod]
    public void ShouldSendNextOrder_OrderCreatedFromInitialized_ReturnsTrue()
    {
        var saga = CreateSaga();
        saga.Order.State = State.Created;
        saga.Order.PreviousState = State.Initialized;

        saga.ShouldSendNextOrder.ShouldBeTrue();
    }

    [TestMethod]
    public void OrderStartedRunning_RunningWithPreviousCreated_ReturnsTrue()
    {
        var saga = CreateSaga();
        saga.Order.State = State.Running;
        saga.Order.PreviousState = State.Created;

        saga.OrderStartedRunning.ShouldBeTrue();
    }

    [TestMethod]
    public void OrderStartedRunning_RunningWithoutPreviousCreated_ReturnsFalse()
    {
        var saga = CreateSaga();
        saga.Order.State = State.Running;
        saga.Order.PreviousState = null;

        saga.OrderStartedRunning.ShouldBeFalse();
    }

    [TestMethod]
    public void OrderStartedRunning_NotRunning_ReturnsFalse()
    {
        var saga = CreateSaga();
        saga.Initialize();
        // After Initialize, state is Created, not Running

        saga.OrderStartedRunning.ShouldBeFalse();
    }

    [TestMethod]
    public void FinishedOrder_FinishedFromRunningAndCompleted_ReturnsTrue()
    {
        var saga = CreateSaga();
        saga.Initialize();
        var lastNode = saga.Order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last();
        var action = lastNode.Actions.First();
        saga.Order.State = State.Finished;
        saga.Order.PreviousState = State.Running;
        saga.Order.Status = OrderGenerator.GenerateOrderState(saga.Order.OrderId, 0, lastNode.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Finished }
        ]);

        saga.IsFinished.ShouldBeTrue();
    }

    [TestMethod]
    public void FinishedOrder_NotFinished_ReturnsFalse()
    {
        var saga = CreateSaga();
        saga.Initialize();
        saga.Order.State = State.Running;
        saga.Order.PreviousState = State.Created;

        saga.IsFinished.ShouldBeFalse();
    }

    [TestMethod]
    public void FinishedOrder_FinishedWithoutPreviousRunning_ReturnsTrue()
    {
        var saga = CreateSaga();
        var lastNode = saga.Order.PlannedRoute.Nodes.OrderBy(n => n.SequenceId).Last();
        var action = lastNode.Actions.First();
        saga.Order.State = State.Finished;
        saga.Order.PreviousState = null;
        saga.Order.Status = OrderGenerator.GenerateOrderState(saga.Order.OrderId, 0, lastNode.SequenceId, false, [
            new OrderActionState { ActionId = action.ActionId, ActionStatus = VehicleActionStatus.Finished }
        ]);

        saga.IsFinished.ShouldBeTrue();
    }

    [TestMethod]
    public void TryUpdateState_HappyPath_ReturnsTrue()
    {
        var saga = CreateSaga();
        saga.Initialize();
        var state = OrderGenerator.GenerateOrderState(saga.Order.OrderId, 0, 0, false);

        saga.TryUpdateState(state).ShouldBeTrue();
    }

    [TestMethod]
    public void TryUpdateState_HappyPath_TransitionsOrderToRunning()
    {
        var saga = CreateSaga();
        saga.Initialize();
        var state = OrderGenerator.GenerateOrderState(saga.Order.OrderId, 0, 0, false);

        saga.TryUpdateState(state);

        saga.Order.State.ShouldBe(State.Running);
    }

    [TestMethod]
    public void PrepareInstantAction_SetsNonNullInstantActionId()
    {
        var saga = CreateSaga();

        saga.PrepareInstantAction();

        saga.InstantActionId.ShouldNotBeNull();
    }

    [TestMethod]
    public void PrepareInstantAction_IncrementsInstantActionsHeaderId()
    {
        var saga = CreateSaga();
        uint counterBefore = saga.InstantActionsHeaderId.Current;

        saga.PrepareInstantAction();

        saga.InstantActionsHeaderId.Current.ShouldBe(counterBefore + 1);
    }

    [TestMethod]
    public void PrepareInstantAction_CalledTwice_GeneratesDifferentIds()
    {
        var saga = CreateSaga();
        saga.PrepareInstantAction();
        string? firstId = saga.InstantActionId;

        saga.PrepareInstantAction();

        saga.InstantActionId.ShouldNotBe(firstId);
    }

    [TestMethod]
    public void ResetInstantAction_ClearsInstantActionId()
    {
        var saga = CreateSaga();
        saga.PrepareInstantAction();
        saga.InstantActionId.ShouldNotBeNull();

        saga.ResetInstantAction();

        saga.InstantActionId.ShouldBeNull();
    }

    [TestMethod]
    public void TryUpdate_ValidRoute_ReturnsTrue()
    {
        var saga = CreateSaga();
        saga.Initialize();
        // Stitching node is first planned node "map/NODE-10" (no Status yet), so new route must contain it
        var newOrder = new Order { PlannedRoute = RouteGenerator.GenerateFromNodeIds(["map/NODE-10", "map/NODE-11", "PS01"]) };

        saga.TryUpdate(newOrder, Guid.NewGuid()).ShouldBeTrue();
    }

    [TestMethod]
    public void TryUpdate_ValidRoute_UpdatesCurrentFleetManagerActionId()
    {
        var saga = CreateSaga();
        saga.Initialize();
        var newFleetManagerActionId = Guid.NewGuid();
        var newOrder = new Order { PlannedRoute = RouteGenerator.GenerateFromNodeIds(["map/NODE-10", "map/NODE-11", "PS01"]) };

        saga.TryUpdate(newOrder, newFleetManagerActionId);

        saga.CurrentFleetManagerActionId.ShouldBe(newFleetManagerActionId);
    }

    [TestMethod]
    public void TryUpdate_ValidRoute_SetsRouteUpdateRequestedFalse()
    {
        var saga = CreateSaga();
        saga.Initialize();
        var newOrder = new Order { PlannedRoute = RouteGenerator.GenerateFromNodeIds(["map/NODE-10", "map/NODE-11", "PS01"]) };

        saga.TryUpdate(newOrder, Guid.NewGuid());

        saga.RouteUpdateRequested.ShouldBeFalse();
    }

    [TestMethod]
    public void TryUpdate_StitchingNodeNotFound_ReturnsFalse()
    {
        var saga = CreateSaga();
        saga.Initialize();
        var newOrder = new Order { PlannedRoute = RouteGenerator.GenerateFromNodeIds(["X", "Y", "Z"]) };

        saga.TryUpdate(newOrder, Guid.NewGuid()).ShouldBeFalse();
    }

    [TestMethod]
    public void TryUpdate_StitchingNodeNotFound_SetsErrorReference()
    {
        var saga = CreateSaga();
        saga.Initialize();
        var newOrder = new Order { PlannedRoute = RouteGenerator.GenerateFromNodeIds(["X", "Y", "Z"]) };

        saga.TryUpdate(newOrder, Guid.NewGuid());

        saga.ErrorReference.ShouldBe(OrderErrors.OrderRouteUpdateStitchingNodeNotFound.Description);
    }

    [TestMethod]
    public void TryUpdate_StitchingNodeNotFound_SetsRouteUpdateRequestedFalse()
    {
        var saga = CreateSaga();
        saga.Initialize();
        var newOrder = new Order { PlannedRoute = RouteGenerator.GenerateFromNodeIds(["X", "Y", "Z"]) };

        saga.TryUpdate(newOrder, Guid.NewGuid());

        saga.RouteUpdateRequested.ShouldBeFalse();
    }

    [TestMethod]
    public void Restart_ReturnsTrue_WhenOrderIsValid()
    {
        var saga = CreateSaga();
        saga.Initialize();

        saga.Restart().ShouldBeTrue();
    }

    [TestMethod]
    public void Restart_ClearsErrorReference()
    {
        var saga = CreateSaga();
        saga.Initialize();
        // Set ErrorReference by triggering a failed TryUpdate
        saga.TryUpdate(new Order { PlannedRoute = RouteGenerator.GenerateFromNodeIds(["X", "Y", "Z"]) }, Guid.NewGuid());
        saga.ErrorReference.ShouldNotBeNull();

        saga.Restart();

        saga.ErrorReference.ShouldBeNull();
    }

    [TestMethod]
    public void Restart_ClearsRouteUpdateRequested()
    {
        var saga = CreateSaga();
        saga.Initialize();
        saga.RouteUpdateRequested = true;

        saga.Restart();

        saga.RouteUpdateRequested.ShouldBeFalse();
    }

    [TestMethod]
    public void Restart_ClearsInstantActionId()
    {
        var saga = CreateSaga();
        saga.Initialize();
        saga.PrepareInstantAction();
        saga.InstantActionId.ShouldNotBeNull();

        saga.Restart();

        saga.InstantActionId.ShouldBeNull();
    }

    [TestMethod]
    public void NewBaseRequested_WhenNewBaseNotified_ReturnsFalse()
    {
        var saga = CreateSaga();
        saga.Initialize();
        saga.TryUpdateState(OrderGenerator.GenerateOrderState(saga.Order.OrderId, 0, 0, true));
        saga.NewBaseNotified = true;

        saga.NewBaseRequested.ShouldBeFalse();
    }

    [TestMethod]
    public void NewBaseRequested_WhenNotNotified_ReturnsTrue()
    {
        var saga = CreateSaga();
        saga.Initialize();
        saga.TryUpdateState(OrderGenerator.GenerateOrderState(saga.Order.OrderId, 0, 0, true));

        saga.NewBaseRequested.ShouldBeTrue();
    }

    [TestMethod]
    public void Restart_ResetsNewBaseNotified()
    {
        var saga = CreateSaga();
        saga.Initialize();
        saga.NewBaseNotified = true;

        saga.Restart();

        saga.NewBaseNotified.ShouldBeFalse();
    }

    [TestMethod]
    public void Restart_ResetsOrderAndReInitializes()
    {
        var saga = CreateSaga();
        saga.Initialize();
        saga.TryUpdateState(OrderGenerator.GenerateOrderState(saga.Order.OrderId, 0, 0, false));
        saga.Order.State.ShouldBe(State.Running);

        saga.Restart();

        saga.Order.State.ShouldBe(State.Created);
        saga.Order.OrderUpdateId.ShouldBe(0);
    }
}