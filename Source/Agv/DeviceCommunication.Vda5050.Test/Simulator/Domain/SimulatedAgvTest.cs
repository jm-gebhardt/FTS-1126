using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.States;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.HeaderIds;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using Moq;
using Shouldly;
using AgvPosition = Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.AgvPosition;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.SimulatedAgvStateMachineTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain;

[TestClass]
public class SimulatedAgvTest
{
    // minute=1 keeps _timeProvider.GetUtcNow().Minute % 5 != 0, which prevents
    // error generation from interfering with assertions unrelated to Error.
    private static readonly DateTimeOffset _baseTime = new(2025, 1, 1, 0, 1, 0, TimeSpan.Zero);

    private sealed class FakeTimeProvider(DateTimeOffset initialTime) : TimeProvider
    {
        private DateTimeOffset _now = initialTime;

        public void Advance(TimeSpan duration)
        {
            _now += duration;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }

    private SimulatedAgv CreateAgv(
        int initialBatteryCharge = 50,
        double chargeRate = 0.1,
        double dischargeRate = 0.1,
        float speed = 1.0f,
        TimeProvider? timeProvider = null,
        AgvOperatingMode operatingMode = AgvOperatingMode.Automatic)
    {
        var agv = new SimulatedAgv(
            Guid.NewGuid(),
            new AgvConfiguration("AGV-001", Fixture.MapId, Fixture.Manufacturer, speed),
            initialBatteryCharge,
            chargeRate,
            dischargeRate,
            timeProvider ?? new FakeTimeProvider(_baseTime),
            new Mock<ISequenceCounter<IStateHeaderId>>().Object,
            new Mock<ISequenceCounter<IVisualizationHeaderId>>().Object,
            new Mock<ISequenceCounter<IConnectionHeaderId>>().Object,
            new Mock<INurbsEdgeBuilder>().Object
        );
        agv.SwitchOperatingMode(operatingMode);
        return agv;
    }

    /// <summary>
    /// Creates an order whose nodes are evenly spaced along the X axis.
    /// Each edge has <paramref name="edgeLength"/> m of length; EstimatedTravelTime = length / speed.
    /// </summary>
    private static Order CreateOrder(string[] nodeIds, double edgeLength = 10.0, ICollection<OrderAction>? actionsOnLastNode = null)
    {
        var nodes = nodeIds.Select((id, i) => new OrderNode
        {
            NodeId = id,
            SequenceId = i * 2,
            Released = true,
            NodePosition = new OrderNodePosition { X = i * edgeLength, Y = 0 },
            Actions = i == nodeIds.Length - 1 && actionsOnLastNode != null ? actionsOnLastNode : []
        }).ToList();

        var edges = new List<OrderEdge>();
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            edges.Add(new OrderEdge
            {
                EdgeId = $"e{i}",
                SequenceId = i * 2 + 1,
                Released = true,
                StartNodeId = nodes[i].NodeId,
                EndNodeId = nodes[i + 1].NodeId,
                Length = edgeLength
            });
        }

        return new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            FullRoute = new Route(nodes, edges)
        };
    }

    [TestMethod]
    public void Constructor_SetsIsOnlineToTrue()
    {
        CreateAgv().IsOnline.ShouldBeTrue();
    }

    [TestMethod]
    public void Constructor_SetsBatteryChargeToInitialValue()
    {
        CreateAgv(initialBatteryCharge: 75).SimulatedBatteryCharge.ShouldBe(75);
    }

    [TestMethod]
    public void Constructor_SetsCurrentPositionToOrigin()
    {
        var agv = CreateAgv();

        agv.CurrentPosition.X.ShouldBe(0f);
        agv.CurrentPosition.Y.ShouldBe(0f);
        agv.CurrentPosition.PositionInitialized.ShouldBeTrue();
    }

    [TestMethod]
    public void Constructor_UsesProvidedCurrentState()
    {
        var agv = new SimulatedAgv(
            Guid.NewGuid(),
            new AgvConfiguration("AGV-001", Fixture.MapId, Fixture.Manufacturer),
            50, 0.1, 0.1,
            new FakeTimeProvider(_baseTime),
            new Mock<ISequenceCounter<IStateHeaderId>>().Object,
            new Mock<ISequenceCounter<IVisualizationHeaderId>>().Object,
            new Mock<ISequenceCounter<IConnectionHeaderId>>().Object,
            new Mock<INurbsEdgeBuilder>().Object,
            currentState: "Idle");

        agv.CurrentState.ShouldBe("Idle");
    }

    [TestMethod]
    public void InitPosition_UpdatesCurrentPosition()
    {
        var agv = CreateAgv();

        agv.InitPosition(new AgvPosition { X = 5, Y = 10, Z = 0, Theta = 0, PositionInitialized = true });

        agv.CurrentPosition.X.ShouldBe(5f);
        agv.CurrentPosition.Y.ShouldBe(10f);
    }

    [TestMethod]
    public void InitPosition_SetsLastNodeId()
    {
        var agv = CreateAgv();

        agv.InitPosition(new AgvPosition { X = 0, Y = 0, Z = 0, Theta = 0, PositionInitialized = true }, "node-5");

        agv.LastNodeId.ShouldBe("node-5");
    }

    [TestMethod]
    public void InitPosition_ResetsLastNodeSequenceIdToZero()
    {
        var agv = CreateAgv();
        agv.LastNodeSequenceId = 6;

        agv.InitPosition(new AgvPosition { X = 0, Y = 0, Z = 0, Theta = 0, PositionInitialized = true });

        agv.LastNodeSequenceId.ShouldBe(0);
    }

    [TestMethod]
    public void UpdateStatus_WhenCharging_IncreasesBatteryByChargeRate()
    {
        // 10 s × 1.0 %/s = +10 % → 50 + 10 = 60
        var agv = CreateAgv(initialBatteryCharge: 50, chargeRate: 1.0);
        agv.Charging = true;

        agv.UpdateStatus(10);

        agv.SimulatedBatteryCharge.ShouldBe(60);
    }

    [TestMethod]
    public void UpdateStatus_WhenNotCharging_ReducesBatteryByDischargeRate()
    {
        // 10 s × 1.0 %/s = −10 % → 50 − 10 = 40
        var agv = CreateAgv(initialBatteryCharge: 50, dischargeRate: 1.0);
        agv.Charging = false;

        agv.UpdateStatus(10);

        agv.SimulatedBatteryCharge.ShouldBe(40);
    }

    [TestMethod]
    public void UpdateStatus_WhenCharging_ClampsAtMaxCharge()
    {
        var agv = CreateAgv(initialBatteryCharge: 99, chargeRate: 10.0);
        agv.Charging = true;

        agv.UpdateStatus(10); // +100 % → clamped to 100

        agv.SimulatedBatteryCharge.ShouldBe(SimulatedAgv.MaxCharge);
    }

    [TestMethod]
    public void UpdateStatus_WhenDischarging_ClampsAtZero()
    {
        var agv = CreateAgv(initialBatteryCharge: 5, dischargeRate: 10.0);
        agv.Charging = false;

        agv.UpdateStatus(10); // −100 % → clamped to 0

        agv.SimulatedBatteryCharge.ShouldBe(0);
    }

    [TestMethod]
    public void StartOrder_SetsCurrentOrder()
    {
        var agv = CreateAgv();
        var order = CreateOrder(["A", "B"]);

        agv.StartOrder(order);

        agv.CurrentOrder.ShouldNotBeNull();
        agv.CurrentOrder!.OrderId.ShouldBe(order.OrderId);
    }

    [TestMethod]
    public void StartOrder_SetsLastNodeIdToFirstNode()
    {
        var agv = CreateAgv();

        agv.StartOrder(CreateOrder(["A", "B", "C"]));

        agv.LastNodeId.ShouldBe("A");
    }

    [TestMethod]
    public void StartOrder_PopulatesPendingActionStatesAsWaiting()
    {
        var action = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var node = new OrderNode { NodeId = "A", SequenceId = 0, Released = true, Actions = [action] };
        var order = new Order { OrderId = "o1", FullRoute = new Route([node], []) };
        var agv = CreateAgv();

        agv.StartOrder(order);

        var state = agv.CurrentOrder!.PendingActionStates.Single(s => s.ActionId == action.ActionId);
        state.ActionStatus.ShouldBe(VehicleActionStatus.Waiting);
    }

    [TestMethod]
    public void UpdateOrder_SetsCurrentOrder()
    {
        var agv = CreateAgv();
        var order = CreateOrder(["A", "B"]);

        agv.UpdateOrder(order);

        agv.CurrentOrder!.OrderId.ShouldBe(order.OrderId);
    }

    [TestMethod]
    public void UpdateOrder_WhileMidEdge_ContinuesFromCurrentPositionInsteadOfRestarting()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);

        var initialOrder = CreateOrder(["A", "B", "C"], edgeLength: 10);
        agv.StartOrder(initialOrder);

        timeProvider.Advance(TimeSpan.FromSeconds(15));
        agv.ProceedOrder();
        agv.CurrentPosition.X.ShouldBe(15f);
        agv.LastNodeId.ShouldBe("B");

        var updatedOrder = CreateOrder(["A", "B", "C", "D"], edgeLength: 10);
        updatedOrder.OrderId = initialOrder.OrderId;
        updatedOrder.OrderUpdateId = 1;
        agv.UpdateOrder(updatedOrder);

        agv.LastNodeId.ShouldBe("B");

        agv.ProceedOrder();
        agv.CurrentPosition.X.ShouldBe(15f);
        agv.LastNodeId.ShouldBe("B");
    }

    [TestMethod]
    public void ProceedOrder_WhenCurrentOrderIsNull_DoesNothing()
    {
        var agv = CreateAgv();

        Should.NotThrow(() => agv.ProceedOrder());
    }

    [TestMethod]
    public void ProceedOrder_WhenOrderHasNoStartTime_DoesNothing()
    {
        var agv = CreateAgv();
        agv.StartOrder(CreateOrder(["A", "B"]));
        agv.CurrentOrder!.StartTime = null;
        var positionBefore = agv.CurrentPosition;

        agv.ProceedOrder();

        agv.CurrentPosition.ShouldBe(positionBefore);
    }

    [TestMethod]
    public void ProceedOrder_MidEdge_UpdatesCurrentPositionAlongEdge()
    {
        // A(0,0)→B(10,0), edge 10 m at 1 m/s = 10 s. At t=5 s → X=5.
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        agv.StartOrder(CreateOrder(["A", "B"], edgeLength: 10));

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        agv.ProceedOrder();

        agv.CurrentPosition.X.ShouldBe(5f);
        agv.CurrentPosition.Y.ShouldBe(0f);
    }

    [TestMethod]
    public void ProceedOrder_AfterFirstEdge_UpdatesLastNodeId()
    {
        // Three nodes, each edge 10 s. At t=15 s the AGV has passed A→B and is on B→C.
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        agv.StartOrder(CreateOrder(["A", "B", "C"], edgeLength: 10));

        timeProvider.Advance(TimeSpan.FromSeconds(15));
        agv.ProceedOrder();

        agv.LastNodeId.ShouldBe("B");
    }

    [TestMethod]
    public void ProceedOrder_WhileEdgeActionRuns_HoldsPositionOnEdgeInsteadOfSnappingBackToStartNode()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);

        var nodeA = new OrderNode { NodeId = "A", SequenceId = 0, Released = true, NodePosition = new OrderNodePosition { X = 0, Y = 0 } };
        var nodeB = new OrderNode { NodeId = "B", SequenceId = 2, Released = true, NodePosition = new OrderNodePosition { X = 10, Y = 0 } };
        var action = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var edge = new OrderEdge
        {
            EdgeId = "e0",
            SequenceId = 1,
            Released = true,
            StartNodeId = "A",
            EndNodeId = "B",
            Length = 10,
            Actions = [action]
        };
        agv.StartOrder(new Order { OrderId = "o1", FullRoute = new Route([nodeA, nodeB], [edge]) });

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        agv.ProceedOrder();
        agv.CurrentPosition.X.ShouldBe(5f);

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        agv.ProceedOrder();
        agv.CurrentPosition.X.ShouldBe(5f);
    }

    [TestMethod]
    public void ProceedOrder_WhenChargingAndAgvMovesToDifferentNode_StopsCharging()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        agv.StartOrder(CreateOrder(["A", "B"], edgeLength: 10));
        agv.Charging = true;

        timeProvider.Advance(TimeSpan.FromSeconds(15));
        agv.ProceedOrder();

        agv.LastNodeId.ShouldBe("B");
        agv.Charging.ShouldBeFalse();
    }

    [TestMethod]
    public void ProceedOrder_WhenChargingAndAgvStaysOnSameNode_KeepsCharging()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        agv.StartOrder(CreateOrder(["A", "B"], edgeLength: 10));
        agv.Charging = true;

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        agv.ProceedOrder();

        agv.LastNodeId.ShouldBe("A");
        agv.Charging.ShouldBeTrue();
    }

    [TestMethod]
    public void ProceedOrder_WithStartChargingAction_DoesNotAutoFinishAfter10Seconds()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        var startChargingAction = new OrderAction(Vda5050Specifications.ActionTypes.StartCharging, null, BlockingType.NONE, []);
        var node = new OrderNode { NodeId = "CHARGER", SequenceId = 0, Released = true, Actions = [startChargingAction] };
        var order = new Order { OrderId = "charge-1", FullRoute = new Route([node], []) };
        agv.StartOrder(order);

        agv.ProceedOrder();
        agv.CurrentOrder!.CurrentAction.ShouldNotBeNull();
        agv.CurrentOrder.CurrentAction!.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.StartCharging);

        timeProvider.Advance(TimeSpan.FromMinutes(5));
        agv.ProceedOrder();

        agv.CurrentOrder!.CurrentAction.ShouldNotBeNull();
        agv.CurrentOrder.CurrentAction!.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.StartCharging);
        agv.CurrentOrder.PendingActionStates.Single(s => s.ActionId == startChargingAction.ActionId).ActionStatus
            .ShouldBe(VehicleActionStatus.Running);
    }

    [TestMethod]
    public void UpdateStatus_WhenChargingReachesMaxCharge_FinishesStartChargingAction()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(initialBatteryCharge: 95, chargeRate: 10.0, timeProvider: timeProvider);
        var startChargingAction = new OrderAction(Vda5050Specifications.ActionTypes.StartCharging, null, BlockingType.NONE, []);
        var node = new OrderNode { NodeId = "CHARGER", SequenceId = 0, Released = true, Actions = [startChargingAction] };
        agv.StartOrder(new Order { OrderId = "charge-1", FullRoute = new Route([node], []) });
        agv.ProceedOrder();
        agv.Charging = true;

        agv.UpdateStatus(10);

        agv.SimulatedBatteryCharge.ShouldBe(SimulatedAgv.MaxCharge);
        agv.CurrentOrder!.CurrentAction.ShouldBeNull();
        agv.CurrentOrder.PendingActionStates.Single(s => s.ActionId == startChargingAction.ActionId).ActionStatus
            .ShouldBe(VehicleActionStatus.Finished);
    }

    [TestMethod]
    public void StopCharging_WhileStartChargingIsRunning_FinishesActionAndStopsCharging()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        var startChargingAction = new OrderAction(Vda5050Specifications.ActionTypes.StartCharging, null, BlockingType.NONE, []);
        var node = new OrderNode { NodeId = "CHARGER", SequenceId = 0, Released = true, Actions = [startChargingAction] };
        agv.StartOrder(new Order { OrderId = "charge-1", FullRoute = new Route([node], []) });
        agv.ProceedOrder();
        agv.Charging = true;

        agv.StopCharging();

        agv.Charging.ShouldBeFalse();
        agv.CurrentOrder!.CurrentAction.ShouldBeNull();
        agv.CurrentOrder.PendingActionStates.Single(s => s.ActionId == startChargingAction.ActionId).ActionStatus
            .ShouldBe(VehicleActionStatus.Finished);
    }

    [TestMethod]
    public void FinishOrder_WhenCurrentOrderIsNull_DoesNothing()
    {
        var agv = CreateAgv();

        Should.NotThrow(() => agv.FinishOrder());
    }

    [TestMethod]
    public void FinishOrder_WhenActionStillRunning_KeepsCurrentOrder()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(timeProvider: timeProvider);
        agv.StartOrder(CreateOrder(["A", "B"], edgeLength: 10));
        agv.CurrentOrder!.PendingActionStates.Add(new OrderActionState
        {
            ActionId = "a1",
            ActionType = Vda5050Specifications.ActionTypes.Pick,
            ActionStatus = VehicleActionStatus.Running
        });

        agv.FinishOrder();

        agv.CurrentOrder.ShouldNotBeNull();
    }

    [TestMethod]
    public void FinishOrder_WhenAllActionsDone_ClearsCurrentOrder()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(timeProvider: timeProvider);
        agv.StartOrder(CreateOrder(["A", "B"], edgeLength: 10));
        timeProvider.Advance(TimeSpan.FromSeconds(20));

        agv.FinishOrder();

        agv.CurrentOrder.ShouldBeNull();
    }

    [TestMethod]
    public void AbortOrder_WhenCurrentOrderIsNull_DoesNothing()
    {
        var agv = CreateAgv();

        Should.NotThrow(() => agv.AbortOrder("cancel-1"));
    }

    [TestMethod]
    public void AbortOrder_AddsCancelOrderActionStateAsFinished()
    {
        var agv = CreateAgv();
        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.AbortOrder("cancel-1");

        var cancelState = agv.CurrentOrder!.PendingActionStates
            .Single(s => s.ActionType == Vda5050Specifications.ActionTypes.CancelOrder);
        cancelState.ActionId.ShouldBe("cancel-1");
        cancelState.ActionStatus.ShouldBe(VehicleActionStatus.Finished);
    }

    [TestMethod]
    public void Constructor_SetsOperatingModeToAutomatic()
    {
        CreateAgv().OperatingMode.ShouldBe(AgvOperatingMode.Automatic);
    }

    [TestMethod]
    public void Constructor_SetsIsInAutomaticModeToTrue()
    {
        CreateAgv().IsInAutomaticMode.ShouldBeTrue();
    }

    [DataRow(AgvOperatingMode.Manual)]
    [DataRow(AgvOperatingMode.Semiautomatic)]
    [DataRow(AgvOperatingMode.Teachin)]
    [DataRow(AgvOperatingMode.Service)]
    [TestMethod]
    public void SetPosition_UpdatesCurrentPosition(AgvOperatingMode operatingMode)
    {
        var agv = CreateAgv(operatingMode: operatingMode);

        agv.SetPosition(new AgvPosition { X = 3, Y = 4, Z = 0, Theta = 0, PositionInitialized = true });

        agv.CurrentPosition.X.ShouldBe(3f);
        agv.CurrentPosition.Y.ShouldBe(4f);
    }

    [TestMethod]
    public void SetPosition_AutomaticMode_DoesNotResetPosition()
    {
        var agv = CreateAgv();

        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.SetPosition(new AgvPosition { X = 3, Y = 4, Z = 0, Theta = 0, PositionInitialized = true });

        agv.CurrentPosition.X.ShouldNotBe(3f);
        agv.CurrentPosition.Y.ShouldNotBe(4f);
    }

    [TestMethod]
    public void SetPosition_ResetsLastNodeIdAndSequenceId()
    {
        var agv = CreateAgv(operatingMode: AgvOperatingMode.Manual);

        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.SetPosition(new AgvPosition { X = 3, Y = 4, Z = 0, Theta = 0, PositionInitialized = true });

        agv.LastNodeId.ShouldBeNull();
        agv.LastNodeSequenceId.ShouldBe(0);
    }

    [TestMethod]
    public void SwitchOperatingMode_ToManual_SetsOperatingModeAndIsInAutomaticModeToFalse()
    {
        var agv = CreateAgv();

        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.OperatingMode.ShouldBe(AgvOperatingMode.Manual);
        agv.IsInAutomaticMode.ShouldBeFalse();
    }

    [TestMethod]
    public void SwitchOperatingMode_ToManual_ClearsCurrentOrderAndLastNode()
    {
        var agv = CreateAgv();
        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.CurrentOrder.ShouldBeNull();
        agv.LastNodeId.ShouldBeNull();
        agv.LastNodeSequenceId.ShouldBe(0);
    }

    [TestMethod]
    public void SwitchOperatingMode_ToManual_WithNoCurrentOrder_DoesNotThrow()
    {
        var agv = CreateAgv();

        Should.NotThrow(() => agv.SwitchOperatingMode(AgvOperatingMode.Manual));
    }

    [TestMethod]
    public void SwitchOperatingMode_ToManual_WithRunningAction_AbortsOrderBeforeClearingIt()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        var action = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE, []);
        var node = new OrderNode { NodeId = "A", SequenceId = 0, Released = true, Actions = [action] };
        var order = new Order { OrderId = "o1", FullRoute = new Route([node], []) };
        agv.StartOrder(order);
        agv.ProceedOrder();
        agv.CurrentOrder!.CurrentAction.ShouldNotBeNull();
        var runningOrder = agv.CurrentOrder;

        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.CurrentOrder.ShouldBeNull();
        runningOrder.PendingActionStates
            .Single(s => s.ActionType == Vda5050Specifications.ActionTypes.CancelOrder)
            .ActionStatus.ShouldBe(VehicleActionStatus.Finished);
    }

    [TestMethod]
    public void SwitchOperatingMode_BackToAutomatic_ClearsMobileRobotNotAvailableError()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);
        agv.StartOrder(CreateOrder(["A", "B"]));
        agv.StartOrder(CreateOrder(["A", "B"]));
        agv.ActualErrors.ShouldContain(e => e.ErrorType == Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable);

        agv.SwitchOperatingMode(AgvOperatingMode.Automatic);

        agv.ActualErrors.ShouldNotContain(e => e.ErrorType == Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable);
    }

    [TestMethod]
    public void SwitchOperatingMode_ToManualThenAutomatic_RestoresIsInAutomaticMode()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.SwitchOperatingMode(AgvOperatingMode.Automatic);

        agv.IsInAutomaticMode.ShouldBeTrue();
    }

    [TestMethod]
    public void InitPosition_WhenNotInAutomaticMode_DoesNotUpdatePosition()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);
        var positionBefore = agv.CurrentPosition;

        agv.InitPosition(new AgvPosition { X = 5, Y = 10, Z = 0, Theta = 0, PositionInitialized = true });

        agv.CurrentPosition.ShouldBe(positionBefore);
    }

    [TestMethod]
    public void InitPosition_WhenNotInAutomaticMode_GeneratesAvailabilityError()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.InitPosition(new AgvPosition { X = 5, Y = 10, Z = 0, Theta = 0, PositionInitialized = true });

        agv.ActualErrors.ShouldHaveSingleItem().ErrorType.ShouldBe(Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable);
    }

    [TestMethod]
    public void StartOrder_WhenNotInAutomaticMode_DoesNotSetCurrentOrder()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.CurrentOrder.ShouldBeNull();
    }

    [TestMethod]
    public void StartOrder_WhenNotInAutomaticMode_GeneratesAvailabilityError()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.ActualErrors.ShouldHaveSingleItem().ErrorType.ShouldBe(Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable);
    }

    [TestMethod]
    public void UpdateOrder_WhenNotInAutomaticMode_DoesNotSetCurrentOrder()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.UpdateOrder(CreateOrder(["A", "B"]));

        agv.CurrentOrder.ShouldBeNull();
    }

    [TestMethod]
    public void UpdateOrder_WhenNotInAutomaticMode_GeneratesAvailabilityError()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.UpdateOrder(CreateOrder(["A", "B"]));

        agv.ActualErrors.ShouldHaveSingleItem().ErrorType.ShouldBe(Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable);
    }

    [TestMethod]
    public void StartOrder_TwiceWhenNotInAutomaticMode_KeepsSingleAvailabilityError()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);

        agv.StartOrder(CreateOrder(["A", "B"]));
        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.ActualErrors.ShouldHaveSingleItem().ErrorType.ShouldBe(Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable);
    }

    [TestMethod]
    public void SetFactsheetAvailable_False_AddsFatalFactsheetError()
    {
        var agv = CreateAgv();

        agv.SetFactsheetAvailable(false);

        var error = agv.ActualErrors.ShouldHaveSingleItem();
        error.ErrorType.ShouldBe(Vda5050Specifications.Errors.FactsheetNotExisting);
        error.ErrorLevel.ShouldBe(ErrorLevel.Fatal);
        error.ErrorDescription.ShouldBe(Fixture.FactsheetNotExistingDescription);
    }

    [TestMethod]
    public void SetFactsheetAvailable_FalseTwice_KeepsFirstFactsheetError()
    {
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(timeProvider: timeProvider);
        agv.SetFactsheetAvailable(false);
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        agv.SetFactsheetAvailable(false);

        agv.ActualErrors.ShouldHaveSingleItem().ErrorReceived.ShouldBe(_baseTime.UtcDateTime);
    }

    [TestMethod]
    public void SetFactsheetAvailable_TrueAfterMissing_RemovesFactsheetError()
    {
        var agv = CreateAgv();
        agv.SetFactsheetAvailable(false);

        agv.SetFactsheetAvailable(true);

        agv.ActualErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public void SetFactsheetAvailable_True_KeepsAvailabilityError()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);
        agv.StartOrder(CreateOrder(["A", "B"]));
        agv.SetFactsheetAvailable(false);

        agv.SetFactsheetAvailable(true);

        agv.ActualErrors.ShouldHaveSingleItem().ErrorType.ShouldBe(Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable);
    }

    [TestMethod]
    public void SwitchOperatingMode_BackToAutomatic_KeepsFactsheetError()
    {
        var agv = CreateAgv();
        agv.SetFactsheetAvailable(false);
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);
        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.SwitchOperatingMode(AgvOperatingMode.Automatic);

        agv.ActualErrors.ShouldHaveSingleItem().ErrorType.ShouldBe(Vda5050Specifications.Errors.FactsheetNotExisting);
    }

    [TestMethod]
    public void Errors_AvailabilityAndFactsheetError_ContainsBoth()
    {
        var agv = CreateAgv();
        agv.SwitchOperatingMode(AgvOperatingMode.Manual);
        agv.StartOrder(CreateOrder(["A", "B"]));

        agv.SetFactsheetAvailable(false);

        agv.Errors.Select(e => e.ErrorType).ShouldBe(
            [Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable, Vda5050Specifications.Errors.FactsheetNotExisting],
            true);
        agv.RandomError.ShouldBeNull();
    }

    [TestMethod]
    public void RequestFactsheet_ValidData_AddsWaitingInstantActionState()
    {
        var agv = CreateAgv();

        agv.RequestFactsheet(MqttInstantActionsGenerator.FactsheetRequestActionId);

        var actionState = agv.InstantActionStates.ShouldHaveSingleItem();
        actionState.ActionId.ShouldBe(MqttInstantActionsGenerator.FactsheetRequestActionId);
        actionState.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.RequestFactsheet);
        actionState.ActionStatus.ShouldBe(VehicleActionStatus.Waiting);
        agv.HasWaitingFactsheetRequests.ShouldBeTrue();
    }

    [TestMethod]
    public void HasWaitingFactsheetRequests_NoRequest_ReturnsFalse()
    {
        CreateAgv().HasWaitingFactsheetRequests.ShouldBeFalse();
    }

    [TestMethod]
    public void FinishFactsheetRequests_TwoWaitingRequests_FinishesBoth()
    {
        var agv = CreateAgv();
        agv.RequestFactsheet(MqttInstantActionsGenerator.FactsheetRequestActionId);
        agv.RequestFactsheet(MqttInstantActionsGenerator.SecondFactsheetRequestActionId);

        agv.FinishFactsheetRequests();

        agv.InstantActionStates.ShouldAllBe(s => s.ActionStatus == VehicleActionStatus.Finished);
        agv.HasWaitingFactsheetRequests.ShouldBeFalse();
    }

    [TestMethod]
    public void RemoveFinishedInstantActionStates_FinishedAndWaitingRequest_KeepsOnlyWaiting()
    {
        var agv = CreateAgv();
        agv.RequestFactsheet(MqttInstantActionsGenerator.FactsheetRequestActionId);
        agv.FinishFactsheetRequests();
        agv.RequestFactsheet(MqttInstantActionsGenerator.SecondFactsheetRequestActionId);

        agv.RemoveFinishedInstantActionStates();

        agv.InstantActionStates.ShouldHaveSingleItem().ActionId.ShouldBe(MqttInstantActionsGenerator.SecondFactsheetRequestActionId);
    }

    [TestMethod]
    public void AddLoad_AddsLoadWithTypeAndPosition()
    {
        const string loadId = "Load1";
        const string loadType = "pallet";
        const string loadPosition = "front";
        var agv = CreateAgv();

        agv.AddLoad(loadId, loadType, loadPosition);

        var load = agv.Loads.ShouldHaveSingleItem();
        load.Id.ShouldBe(loadId);
        load.LoadType.ShouldBe(loadType);
        load.LoadPosition.ShouldBe(loadPosition);
        load.PickStartTime.ShouldBe(_baseTime.DateTime);
    }

    [TestMethod]
    public void AddLoad_WhenPositionAlreadyOccupied_DoesNotAddSecondLoad()
    {
        const string loadPosition = "front";
        var agv = CreateAgv();
        agv.AddLoad("Load1", "pallet", loadPosition);

        agv.AddLoad("Load2", "box", loadPosition);

        agv.Loads.Count.ShouldBe(1);
    }

    [TestMethod]
    public void AddLoad_WithDifferentPositions_AddsBoth()
    {
        var agv = CreateAgv();
        agv.AddLoad("Load1", "pallet", "front");

        agv.AddLoad("Load2", "box", "back");

        agv.Loads.Count.ShouldBe(2);
    }

    [TestMethod]
    public void RemoveLoad_RemovesLoadMatchingId()
    {
        const string loadId = "Load1";
        var agv = CreateAgv();
        agv.AddLoad(loadId, "pallet", "front");

        agv.RemoveLoad(loadId);

        agv.Loads.ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveLoad_WhenIdAbsent_DoesNothing()
    {
        var agv = CreateAgv();
        agv.AddLoad("Load1", "pallet", "front");

        agv.RemoveLoad("UNKNOWN");

        agv.Loads.Count.ShouldBe(1);
    }

    [TestMethod]
    public void ProceedOrder_WhenPickActionFinishes_AddsLoadWithMappedTypeAndPosition()
    {
        const string loadType = "pallet";
        const string loadPosition = "LHD1";
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        var pickAction = new OrderAction(Vda5050Specifications.ActionTypes.Pick, null, BlockingType.NONE,
        [
            new OrderActionParameter { Key = Vda5050Specifications.Parameters.LoadHandlingDevice, Value = loadPosition },
            new OrderActionParameter { Key = Vda5050Specifications.Parameters.LoadType, Value = loadType }
        ]);
        agv.StartOrder(CreateOrder(["A"], actionsOnLastNode: [pickAction]));

        agv.ProceedOrder();
        timeProvider.Advance(TimeSpan.FromSeconds(11));
        agv.ProceedOrder();

        var load = agv.Loads.ShouldHaveSingleItem();
        load.LoadType.ShouldBe(loadType);
        load.LoadPosition.ShouldBe(loadPosition);
        load.Id.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void ProceedOrder_WhenDropActionFinishes_RemovesLoadMatchedByPosition()
    {
        const string loadPosition = "LHD1";
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(speed: 1.0f, timeProvider: timeProvider);
        agv.AddLoad("Load1", "pallet", loadPosition);
        var dropAction = new OrderAction(Vda5050Specifications.ActionTypes.Drop, null, BlockingType.NONE,
        [
            new OrderActionParameter { Key = Vda5050Specifications.Parameters.LoadHandlingDevice, Value = loadPosition }
        ]);
        agv.StartOrder(CreateOrder(["A"], actionsOnLastNode: [dropAction]));

        agv.ProceedOrder();
        timeProvider.Advance(TimeSpan.FromSeconds(11));
        agv.ProceedOrder();

        agv.Loads.ShouldBeEmpty();
    }
}