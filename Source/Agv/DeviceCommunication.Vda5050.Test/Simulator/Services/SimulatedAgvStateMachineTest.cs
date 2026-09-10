using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
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
    private static AgvConfiguration CreateConfiguration(string serialNumber = "simagv0",
        int stateUpdateIntervalMs = SimulatedAgvStateMachineTestFixture.StateUpdateIntervalMs,
        int visualizationUpdateIntervalMs = SimulatedAgvStateMachineTestFixture.VisualizationUpdateIntervalMs,
        double batteryChargeRate = SimulatedAgvTypeConfiguration.DefaultBatteryChargeRate,
        double batteryDischargeRate = SimulatedAgvTypeConfiguration.DefaultBatteryDischargeRate)
    {
        return new AgvConfiguration(serialNumber, SimulatedAgvStateMachineTestFixture.MapId, SimulatedAgvStateMachineTestFixture.Manufacturer)
        {
            StateUpdateIntervalMs = stateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = visualizationUpdateIntervalMs,
            BatteryChargeRate = batteryChargeRate,
            BatteryDischargeRate = batteryDischargeRate
        };
    }

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
        saga.CurrentOrder.ShouldNotBeNull().OrderId.ShouldBe(order.OrderId);
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
        var newPosition = new Vda5050.Simulator.Domain.Models.AgvPosition { X = 5, Y = 10, Z = 0, Theta = 0, PositionInitialized = true };
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
        saga.CurrentOrder.ShouldNotBeNull().OrderUpdateId.ShouldBe(1);
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

    [TestMethod]
    public async Task Consume_CreateAgv_PublishesMqttStateWithAutomaticOperatingMode()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        var state = await fixture.GetFirstPublishedMessageAsync<MqttState>();
        state.OperatingMode.ShouldBe(MqttOperatingMode.Automatic);
    }

    [TestMethod]
    public async Task Consume_SetToPosition_UpdatesSagaPosition()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));
        var newPosition = new Vda5050.Simulator.Domain.Models.AgvPosition { X = 5, Y = 10, Z = 0, Theta = 0, PositionInitialized = true };
        await fixture.PublishMessageAsync(new SetToPosition(config.CorrelationId, newPosition));

        var saga = fixture.GetSaga(config);
        saga.CurrentPosition.X.ShouldBe(5.0f);
        saga.CurrentPosition.Y.ShouldBe(10.0f);
    }

    [TestMethod]
    public async Task Consume_SetToPosition_ResetsLastNodeId()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        var initialPosition = new Vda5050.Simulator.Domain.Models.AgvPosition { X = 0, Y = 0, Z = 0, Theta = 0, PositionInitialized = true };
        await fixture.PublishMessageAsync(new UpdatePosition(config.CorrelationId, initialPosition, "node1"));
        fixture.GetSaga(config).LastNodeId.ShouldBe("node1");

        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));
        var newPosition = new Vda5050.Simulator.Domain.Models.AgvPosition { X = 5, Y = 10, Z = 0, Theta = 0, PositionInitialized = true };
        await fixture.PublishMessageAsync(new SetToPosition(config.CorrelationId, newPosition));

        var saga = fixture.GetSaga(config);
        saga.LastNodeId.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_SetToPosition_PublishesMqttVisualizationWithNewPosition()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));
        var newPosition = new Vda5050.Simulator.Domain.Models.AgvPosition { X = 5, Y = 10, Z = 0, Theta = 0, PositionInitialized = true };
        await fixture.PublishMessageAsync(new SetToPosition(config.CorrelationId, newPosition));

        ICollection<MqttVisualization> visualizations = fixture.GetPublishedMessages<MqttVisualization>();
        visualizations.Count.ShouldBeGreaterThanOrEqualTo(2);
        visualizations.Last().AgvPosition.ShouldNotBeNull().X.ShouldBe(5.0f);
        visualizations.Last().AgvPosition.ShouldNotBeNull().Y.ShouldBe(10.0f);
    }

    [TestMethod]
    public async Task Consume_SetToPosition_PublishesMqttState()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));
        var newPosition = new Vda5050.Simulator.Domain.Models.AgvPosition { X = 5, Y = 10, Z = 0, Theta = 0, PositionInitialized = true };

        await fixture.PublishMessageAsync(new SetToPosition(config.CorrelationId, newPosition));

        ICollection<MqttState> states = fixture.GetPublishedMessages<MqttState>();
        states.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public async Task Consume_SwitchOperatingMode_UpdatesSagaOperatingMode()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));
        await fixture.GetFirstPublishedMessageAsync<MqttState>(2);

        var saga = fixture.GetSaga(config);
        saga.OperatingMode.ShouldBe(AgvOperatingMode.Manual);
        saga.IsInAutomaticMode.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_SwitchOperatingMode_ToManual_FromBusyMoving_TransitionsToIdle()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new StartOrder(config.CorrelationId, CreateOrder(["A", "B"])));
        fixture.GetSaga(config).CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.BusyMoving));

        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));
        await fixture.GetFirstPublishedMessageAsync<MqttState>(3);

        var saga = fixture.GetSaga(config);
        saga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
    }

    [TestMethod]
    public async Task Consume_SwitchOperatingMode_ToManual_ClearsCurrentOrder()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new StartOrder(config.CorrelationId, CreateOrder(["A", "B"])));

        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));
        await fixture.GetFirstPublishedMessageAsync<MqttState>(3);

        var saga = fixture.GetSaga(config);
        saga.CurrentOrder.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_SwitchOperatingMode_ToAutomatic_FromIdle_StaysIdle()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));

        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Automatic));
        await fixture.GetFirstPublishedMessageAsync<MqttState>(2);

        var saga = fixture.GetSaga(config);
        saga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        saga.IsInAutomaticMode.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_SwitchOperatingMode_PublishesMqttStateWithMappedOperatingMode()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));

        var state = await fixture.GetFirstPublishedMessageAsync<MqttState>(2);
        state.OperatingMode.ShouldBe(MqttOperatingMode.Manual);
    }

    [TestMethod]
    public async Task Consume_SwitchOperatingMode_BackToAutomatic_ClearsMobileRobotNotAvailableError()
    {
        var config = CreateConfiguration("simagv-mode-error");
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Manual));
        var position = new Vda5050.Simulator.Domain.Models.AgvPosition { X = 0, Y = 0, Z = 0, Theta = 0, PositionInitialized = true };
        await fixture.PublishMessageAsync(new UpdatePosition(config.CorrelationId, position));
        fixture.GetSaga(config).Error.ShouldNotBeNull().ErrorType.ShouldBe(Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable);

        await fixture.PublishMessageAsync(new SwitchOperatingMode(config.CorrelationId, AgvOperatingMode.Automatic));
        await fixture.GetFirstPublishedMessageAsync<MqttState>(3);

        var saga = fixture.GetSaga(config);
        saga.Error.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_AddLoad_AddsLoadToSaga()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new AddLoad(config.CorrelationId, SimulatedAgvStateMachineTestFixture.LoadId, SimulatedAgvStateMachineTestFixture.LoadType, SimulatedAgvStateMachineTestFixture.LoadPosition));

        var saga = fixture.GetSaga(config);
        var load = saga.Loads.ShouldHaveSingleItem();
        load.Id.ShouldBe(SimulatedAgvStateMachineTestFixture.LoadId);
        load.LoadType.ShouldBe(SimulatedAgvStateMachineTestFixture.LoadType);
        load.LoadPosition.ShouldBe(SimulatedAgvStateMachineTestFixture.LoadPosition);
    }

    [TestMethod]
    public async Task Consume_AddLoad_PublishesMqttStateWithLoad()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new AddLoad(config.CorrelationId, SimulatedAgvStateMachineTestFixture.LoadId, SimulatedAgvStateMachineTestFixture.LoadType, SimulatedAgvStateMachineTestFixture.LoadPosition));

        ICollection<MqttState> states = fixture.GetPublishedMessages<MqttState>();
        var load = states.Last().Loads.ShouldNotBeNull().ShouldHaveSingleItem();
        load.LoadId.ShouldBe(SimulatedAgvStateMachineTestFixture.LoadId);
        load.LoadType.ShouldBe(SimulatedAgvStateMachineTestFixture.LoadType);
        load.LoadPosition.ShouldBe(SimulatedAgvStateMachineTestFixture.LoadPosition);
    }

    [TestMethod]
    public async Task Consume_RemoveLoad_RemovesLoadFromSaga()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new AddLoad(config.CorrelationId, SimulatedAgvStateMachineTestFixture.LoadId, SimulatedAgvStateMachineTestFixture.LoadType, SimulatedAgvStateMachineTestFixture.LoadPosition));
        await fixture.PublishMessageAsync(new RemoveLoad(config.CorrelationId, SimulatedAgvStateMachineTestFixture.LoadId));

        var saga = fixture.GetSaga(config);
        saga.Loads.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_RemoveLoad_PublishesMqttStateWithoutLoad()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new AddLoad(config.CorrelationId, SimulatedAgvStateMachineTestFixture.LoadId, SimulatedAgvStateMachineTestFixture.LoadType, SimulatedAgvStateMachineTestFixture.LoadPosition));
        await fixture.PublishMessageAsync(new RemoveLoad(config.CorrelationId, SimulatedAgvStateMachineTestFixture.LoadId));

        ICollection<MqttState> states = fixture.GetPublishedMessages<MqttState>();
        states.Last().Loads.ShouldNotBeNull().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_RemoveAgv_PublishesOfflineMqttConnection()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new RemoveAgv(config.CorrelationId));

        var connection = fixture.GetPublishedMessages<MqttConnection>().Last();
        connection.SerialNumber.ShouldBe(config.SerialNumber);
        connection.Manufacturer.ShouldBe(config.Manufacturer);
        connection.ConnectionState.ShouldBe(MqttConnectionState.OFFLINE);
    }

    [TestMethod]
    public async Task Consume_RemoveAgv_SetsAgvOffline()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new RemoveAgv(config.CorrelationId));

        var saga = fixture.GetSaga(config);
        saga.IsOnline.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_RemoveAgv_FinalizesSaga()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new RemoveAgv(config.CorrelationId));

        await fixture.ShouldHaveRemovedSagaAsync(config.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_RemoveAgv_ClearsCachesOfAgv()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new RemoveAgv(config.CorrelationId));

        fixture.ShouldHaveClearedCaches(config.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_UpdateAgvConfiguration_AppliesConfigurationToSaga()
    {
        var config = CreateConfiguration();
        var changedConfig = CreateConfiguration(stateUpdateIntervalMs: SimulatedAgvStateMachineTestFixture.ChangedStateUpdateIntervalMs,
            visualizationUpdateIntervalMs: SimulatedAgvStateMachineTestFixture.ChangedVisualizationUpdateIntervalMs,
            batteryChargeRate: SimulatedAgvStateMachineTestFixture.ChangedBatteryChargeRate,
            batteryDischargeRate: SimulatedAgvStateMachineTestFixture.ChangedBatteryDischargeRate);
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new UpdateAgvConfiguration(changedConfig));

        var saga = fixture.GetSaga(config);
        saga.Configuration.StateUpdateIntervalMs.ShouldBe(SimulatedAgvStateMachineTestFixture.ChangedStateUpdateIntervalMs);
        saga.Configuration.VisualizationUpdateIntervalMs.ShouldBe(SimulatedAgvStateMachineTestFixture.ChangedVisualizationUpdateIntervalMs);
        saga.BatteryChargeRate.ShouldBe(SimulatedAgvStateMachineTestFixture.ChangedBatteryChargeRate);
        saga.BatteryDischargeRate.ShouldBe(SimulatedAgvStateMachineTestFixture.ChangedBatteryDischargeRate);
    }

    [TestMethod]
    public async Task Consume_UpdateAgvConfiguration_KeepsAgvOnline()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new UpdateAgvConfiguration(CreateConfiguration(stateUpdateIntervalMs: SimulatedAgvStateMachineTestFixture.ChangedStateUpdateIntervalMs)));

        var saga = fixture.GetSaga(config);
        saga.IsOnline.ShouldBeTrue();
        saga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
    }

    [TestMethod]
    public async Task Consume_UpdateAgvConfiguration_PublishesMqttState()
    {
        var config = CreateConfiguration();
        var fixture = new SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(2)).Build();

        await fixture.PublishMessageAsync(new CreateAgv(config));
        await fixture.PublishMessageAsync(new UpdateAgvConfiguration(CreateConfiguration(stateUpdateIntervalMs: SimulatedAgvStateMachineTestFixture.ChangedStateUpdateIntervalMs)));

        var state = await fixture.GetFirstPublishedMessageAsync<MqttState>(2);
        state.SerialNumber.ShouldBe(config.SerialNumber);
    }
}