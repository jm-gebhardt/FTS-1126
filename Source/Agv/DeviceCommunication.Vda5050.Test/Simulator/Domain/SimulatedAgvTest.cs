using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.States;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.HeaderIds;
using Moq;
using Shouldly;

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

        public void Advance(TimeSpan duration) => _now += duration;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private SimulatedAgv CreateAgv(
        int initialBatteryCharge = 50,
        double chargeRate = 0.1,
        double dischargeRate = 0.1,
        float speed = 1.0f,
        TimeProvider? timeProvider = null)
    {
        return new SimulatedAgv(
            Guid.NewGuid(),
            new AgvConfiguration("AGV-001", Speed: speed),
            initialBatteryCharge,
            chargeRate,
            dischargeRate,
            timeProvider ?? new FakeTimeProvider(_baseTime),
            new Mock<ISequenceCounter<IStateHeaderId>>().Object,
            new Mock<ISequenceCounter<IVisualizationHeaderId>>().Object,
            new Mock<ISequenceCounter<IConnectionHeaderId>>().Object,
            new Mock<INurbsEdgeBuilder>().Object
        );
    }

    /// <summary>
    /// Creates an order whose nodes are evenly spaced along the X axis.
    /// Each edge has <paramref name="edgeLength"/> m of length; EstimatedTravelTime = length / speed.
    /// </summary>
    private static Order CreateOrder(string[] nodeIds, double edgeLength = 10.0)
    {
        List<OrderNode> nodes = nodeIds.Select((id, i) => new OrderNode
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
            new AgvConfiguration("AGV-001"),
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

        agv.InitPosition(new AgvPosition(true, 5, 10, 0, 0));

        agv.CurrentPosition.X.ShouldBe(5f);
        agv.CurrentPosition.Y.ShouldBe(10f);
    }

    [TestMethod]
    public void InitPosition_SetsLastNodeId()
    {
        var agv = CreateAgv();

        agv.InitPosition(new AgvPosition(true, 0, 0, 0, 0), "node-5");

        agv.LastNodeId.ShouldBe("node-5");
    }

    [TestMethod]
    public void InitPosition_ResetsLastNodeSequenceIdToZero()
    {
        var agv = CreateAgv();
        agv.LastNodeSequenceId = 6;

        agv.InitPosition(new AgvPosition(true, 0, 0, 0, 0));

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
    public void ProceedOrder_WhenCurrentOrderIsNull_DoesNothing()
    {
        var agv = CreateAgv();

        Should.NotThrow(agv.ProceedOrder);
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
    public void FinishOrder_WhenCurrentOrderIsNull_DoesNothing()
    {
        var agv = CreateAgv();

        Should.NotThrow(agv.FinishOrder);
    }

    [TestMethod]
    public void FinishOrder_WhenOrderStillInProgress_KeepsCurrentOrder()
    {
        // Duration = 10 s; finish called at t=5 s → order not yet complete
        var timeProvider = new FakeTimeProvider(_baseTime);
        var agv = CreateAgv(timeProvider: timeProvider);
        agv.StartOrder(CreateOrder(["A", "B"], edgeLength: 10));

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        agv.FinishOrder();

        agv.CurrentOrder.ShouldNotBeNull();
    }

    [TestMethod]
    public void FinishOrder_WhenOrderComplete_ClearsCurrentOrder()
    {
        // Duration = 10 s; finish called at t=20 s → order done
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
}
