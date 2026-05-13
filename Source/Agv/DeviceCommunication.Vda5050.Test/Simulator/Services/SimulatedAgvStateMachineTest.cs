using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.StateMachines;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

[TestClass]
public class SimulatedAgvStateMachineTest
{
    private static AgvConfiguration CreateConfiguration(string serialNumber = "simagv0")
        => new(serialNumber);

    private static Order CreateOrder(string[] nodeIds)
    {
        List<OrderNode> nodes = nodeIds.Select((id, i) => new OrderNode
        {
            NodeId = id,
            SequenceId = i * 2,
            Released = true,
            NodePosition = new OrderNodePosition { X = i * 10.0, Y = 0 }
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
                Length = 10.0
            });
        }

        return new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            FullRoute = new Route(nodes, edges)
        };
    }

    [TestMethod]
    public async Task Consume_CreateAgv_TransitionsToIdle()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        var saga = fixture.GetSaga(config);
        saga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
    }

    [TestMethod]
    public async Task Consume_CreateAgv_PublishesMqttConnection()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        await fixture.ShouldHavePublishedAsync<MqttConnection>();
        var connection = await fixture.GetFirstPublishedMessageAsync<MqttConnection>();
        connection.SerialNumber.ShouldBe(config.SerialNumber);
        connection.Manufacturer.ShouldBe(config.Manufacturer);
        connection.ConnectionState.ShouldBe(MqttConnectionState.ONLINE);
    }

    [TestMethod]
    public async Task Consume_CreateAgv_PublishesMqttState()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        await fixture.ShouldHavePublishedAsync<MqttState>();
        var state = await fixture.GetFirstPublishedMessageAsync<MqttState>();
        state.SerialNumber.ShouldBe(config.SerialNumber);
        state.Manufacturer.ShouldBe(config.Manufacturer);
        state.HeaderId.ShouldBe(1u);
    }

    [TestMethod]
    public async Task Consume_CreateAgv_PublishesMqttVisualization()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        await fixture.ShouldHavePublishedAsync<MqttVisualization>();
        var visualization = await fixture.GetFirstPublishedMessageAsync<MqttVisualization>();
        visualization.SerialNumber.ShouldBe(config.SerialNumber);
        visualization.Manufacturer.ShouldBe(config.Manufacturer);
    }

    [TestMethod]
    public async Task Consume_CreateAgv_SetsIsOnline()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        var saga = fixture.GetSaga(config);
        saga.IsOnline.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartOrder_FromIdle_TransitionsToBusyMoving()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        var order = CreateOrder(["A", "B", "C"]);
        await fixture.PublishMessageAsync(new StartOrder(config.CorrelationId, order));

        var saga = fixture.GetSaga(config);
        saga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.BusyMoving));
    }

    [TestMethod]
    public async Task Consume_StartOrder_SetsCurrentOrder()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        var order = CreateOrder(["A", "B"]);
        await fixture.PublishMessageAsync(new StartOrder(config.CorrelationId, order));

        var saga = fixture.GetSaga(config);
        saga.CurrentOrder.ShouldNotBeNull();
        saga.CurrentOrder!.OrderId.ShouldBe(order.OrderId);
    }

    [TestMethod]
    public async Task Consume_StartOrder_PublishesMqttState()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        var order = CreateOrder(["A", "B"]);
        await fixture.PublishMessageAsync(new StartOrder(config.CorrelationId, order));

        ICollection<MqttState> states = fixture.GetPublishedMessages<MqttState>();
        states.Count.ShouldBeGreaterThanOrEqualTo(2);
        var startOrderState = states.Last();
        startOrderState.OrderId.ShouldBe(order.OrderId);
    }

    [TestMethod]
    public async Task Consume_StartOrder_PublishesMqttVisualization()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        var order = CreateOrder(["A", "B"]);
        await fixture.PublishMessageAsync(new StartOrder(config.CorrelationId, order));

        ICollection<MqttVisualization> visualizations = fixture.GetPublishedMessages<MqttVisualization>();
        visualizations.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public async Task Consume_StartCharging_SetsChargingToTrue()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new StartCharging(config.CorrelationId));

        var saga = fixture.GetSaga(config);
        saga.Charging.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartCharging_MqttStateBatteryStateIsCharging()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new StartCharging(config.CorrelationId));

        ICollection<MqttState> states = fixture.GetPublishedMessages<MqttState>();
        states.Last().BatteryState.Charging.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StopCharging_SetsChargingToFalse()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new StartCharging(config.CorrelationId));
        await fixture.PublishMessageAsync(new StopCharging(config.CorrelationId));

        var saga = fixture.GetSaga(config);
        saga.Charging.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_StopCharging_MqttStateBatteryStateIsNotCharging()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new StartCharging(config.CorrelationId));
        await fixture.PublishMessageAsync(new StopCharging(config.CorrelationId));

        ICollection<MqttState> states = fixture.GetPublishedMessages<MqttState>();
        states.Last().BatteryState.Charging.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_UpdatePosition_UpdatesSagaPosition()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        var newPosition = new AgvPosition(true, 5.0f, 10.0f, 0, 0);
        await fixture.PublishMessageAsync(new UpdatePosition(config.CorrelationId, newPosition, "node1"));

        var saga = fixture.GetSaga(config);
        saga.CurrentPosition.X.ShouldBe(5.0f);
        saga.CurrentPosition.Y.ShouldBe(10.0f);
        saga.LastNodeId.ShouldBe("node1");
    }

    [TestMethod]
    public async Task Consume_UpdateOrder_UpdatesCurrentOrder()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        var initialOrder = CreateOrder(["A", "B"]);
        await fixture.PublishMessageAsync(new StartOrder(config.CorrelationId, initialOrder));

        var updatedOrder = CreateOrder(["A", "B", "C"]);
        updatedOrder.OrderId = initialOrder.OrderId;
        updatedOrder.OrderUpdateId = 1;
        await fixture.PublishMessageAsync(new UpdateOrder(config.CorrelationId, updatedOrder));

        var saga = fixture.GetSaga(config);
        saga.CurrentOrder.ShouldNotBeNull();
        saga.CurrentOrder!.OrderUpdateId.ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_CreateAgv_HeaderIdIncrements()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        var connection = await fixture.GetFirstPublishedMessageAsync<MqttConnection>();
        connection.HeaderId.ShouldBe(1u);

        var state = await fixture.GetFirstPublishedMessageAsync<MqttState>();
        state.HeaderId.ShouldBe(1u);

        var saga = fixture.GetSaga(config);
        saga.VisualizationHeaderId.Current.ShouldBe(1u);
    }

    [TestMethod]
    public async Task Consume_StartOrder_HeaderIdIncrementsOnEachMqttState()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new StartOrder(config.CorrelationId, CreateOrder(["A", "B"])));

        List<MqttState> states = fixture.GetPublishedMessages<MqttState>().OrderBy(s => s.HeaderId).ToList();
        states.Count.ShouldBeGreaterThanOrEqualTo(2);
        states[0].HeaderId.ShouldBe(1u);
        states[1].HeaderId.ShouldBe(2u);
    }

    [TestMethod]
    public async Task Consume_MultipleCreateAgvMessages_EachGetsIndependentSaga()
    {
        var config1 = CreateConfiguration("simagv1");
        var config2 = CreateConfiguration("simagv2");
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config1));
        await fixture.PublishMessageAsync(new CreateAgv(config2));

        var saga1 = fixture.GetSaga(config1);
        var saga2 = fixture.GetSaga(config2);

        saga1.CorrelationId.ShouldNotBe(saga2.CorrelationId);
        saga1.Configuration.SerialNumber.ShouldBe("simagv1");
        saga2.Configuration.SerialNumber.ShouldBe("simagv2");
        saga1.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        saga2.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
    }
}
