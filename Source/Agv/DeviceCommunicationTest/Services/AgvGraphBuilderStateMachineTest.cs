using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services;

/// <summary>
/// Tests for <see cref="AgvGraphBuilderStateMachine"/>.
/// </summary>
[TestClass]
public class AgvGraphBuilderStateMachineTest
{
    private const string AgvSerialNumber = "#2410#";

    [TestMethod]
    public async Task When_RequestAgvGraphReceived_NewAgvGraphBuilderSagaCreated()
    {
        var correlationId = AgvSerialNumber.ToGuid();

        var fixture = new AgvGraphBuilderStateMachineFixture();

        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvSerialNumber));
        (await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        (await fixture.TestHarness.Published.Any<MqttRequestStationList>()).ShouldBeTrue();
        saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.RequestAgvGraphReceived);
        saga.CorrelationId.ShouldBe(correlationId);
        saga.MqttStationList.ShouldBeNull();
        saga.AgvGraph.ShouldBeNull();
    }

    [TestMethod]
    public async Task When_RequestAgvGraphReceived_EmptyCorrelationId_NoSagaCreated()
    {
        var correlationId = Guid.Empty;

        var fixture = new AgvGraphBuilderStateMachineFixture();

        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvSerialNumber));
        (await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>()).ShouldBeFalse();

        (await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10))).ShouldBeNull();
    }

    /// <summary>
    /// When AgvReserve Requested Agv Is Idle AcceptAgvReservation
    /// </summary>
    [TestMethod]
    public async Task When_AllStatesPassedByReceive_ValidGraphPublished()
    {
        var correlationId = AgvSerialNumber.ToGuid();

        var fixture = new AgvGraphBuilderStateMachineFixture();

        // Publish RequestAgvGraph ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvSerialNumber));
        // ... expect it's going to be consumed by the state machine.
        (await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();

        // Get the instance which correlates to the AGV.
        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

        // Ensure that state machine did send MqttRequestStationList ...
        (await fixture.TestHarness.Published.Any<MqttRequestStationList>()).ShouldBeTrue();
        // ... and the current state is RequestAgvGraphReceived.
        saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.RequestAgvGraphReceived);
        saga.MqttStationList.ShouldBeNull();
        saga.MqttGraph.ShouldBeNull();
        saga.AgvGraph.ShouldBeNull();

        var requestIdList = saga.CurrentMqttRequestId; // Has been chosen by the state machine as request ID for MQTT.
        var stationList = new MqttStationList(AgvSerialNumber, DateTimeOffset.UtcNow,
            new Station[]
            {
                new(Function.Pick, "P01", new Position(1f, 2f, 3f), "42"),
                new(Function.Drop, "D01", new Position(4f, 5f, 6f), "7")
            },
            requestIdList);
        // Publish StationListReceived ...
        await fixture.TestHarness.Bus.Publish(new InternalStationListReceived(correlationId, stationList));
        // ... expect it's going to be consumed by the state machine.
        (await fixture.SagaHarness.Consumed.Any<InternalStationListReceived>()).ShouldBeTrue();

        // Ensure that state machine did send MqttRequestGraph ...
        (await fixture.TestHarness.Published.Any<MqttRequestGraph>()).ShouldBeTrue();
        // ... and the current state is StationListReceived.
        saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.StationListReceived);

        saga.MqttStationList.ShouldNotBeNull();
        saga.MqttGraph.ShouldBeNull();
        saga.AgvGraph.ShouldBeNull();
        saga.MqttStationList.SerialNumber.ToGuid().ShouldBe(correlationId);
        saga.MqttStationList.Stations.Length.ShouldBe(2);
        saga.MqttStationList.Stations.Any(x => x.Identifier == "P01").ShouldBeTrue();
        var stationD02 = saga.MqttStationList.Stations.Single(x => x.Identifier == "D01");
        stationD02.TypeId.ShouldBe("7");

        var requestIdGraph = saga.CurrentMqttRequestId; // Has been chosen by the state machine as request ID for MQTT.
        var graph = new MqttGraph(AgvSerialNumber, DateTimeOffset.UtcNow,
            new MqttNode[]
            {
                new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = "2", X = 9f, Y = 9f, Z = 0f }
            },
            new MqttEdge[]
            {
                new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1 }
            },
            string.Empty,
            requestIdGraph);
        // Publish GraphReceived ...
        await fixture.TestHarness.Bus.Publish(new InternalGraphReceived(correlationId, graph));
        // ... expect it will be consumed by the state machine.
        (await fixture.SagaHarness.Consumed.Any<InternalGraphReceived>()).ShouldBeTrue();

        requestIdGraph.ShouldNotBe(requestIdList);

        // Check if the state machine published AgvGraphReceived finally.
        var publishedData = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;

        publishedData.CorrelationId.ShouldBe(correlationId);
        publishedData.Graph.CorrelationId.ShouldBe(AgvSerialNumber.ToGuid());
        // 2 stations + 2 nodes
        publishedData.Graph.Nodes.Count.ShouldBe(2 + 2);
        // 4 edges (2 (bidirectional) per station + 2 edges (bidirectional) from graph
        publishedData.Graph.Edges.Count.ShouldBe(2 + 4);
    }

    /// <summary>
    /// When graph is invalid (e.g. duplicate node names) no graph will be stored
    /// </summary>
    [TestMethod]
    public async Task When_InvalidStationListReceive_InvalidGraphPublished()
    {
        var correlationId = AgvSerialNumber.ToGuid();

        var fixture = new AgvGraphBuilderStateMachineFixture();

        // Publish RequestAgvGraph ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvSerialNumber));
        // ... expect it's going to be consumed by the state machine.
        (await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();

        // Get the instance which correlates to the AGV.
        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

        // Ensure that state machine did send MqttRequestStationList ...
        (await fixture.TestHarness.Published.Any<MqttRequestStationList>()).ShouldBeTrue();
        // ... and the current state is RequestAgvGraphReceived.
        saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.RequestAgvGraphReceived);

        var requestIdList = saga.CurrentMqttRequestId; // Has been chosen by the state machine as request ID for MQTT.
        var stationList = new MqttStationList(AgvSerialNumber, DateTimeOffset.UtcNow,
            new Station[]
            {
                new(Function.Pick, "P01", new Position(1f, 2f, 3f), "42"),
                new(Function.Drop, "D01", new Position(4f, 5f, 6f), "7"),
                new(Function.Drop, "D01", new Position(4f, 5f, 6f), "7") // Duplicate
            },
            requestIdList);
        // Publish StationListReceived ...
        await fixture.TestHarness.Bus.Publish(new InternalStationListReceived(correlationId, stationList));
        // ... expect it's going to be consumed by the state machine.
        (await fixture.SagaHarness.Consumed.Any<InternalStationListReceived>()).ShouldBeTrue();

        // Ensure that state machine did send MqttRequestGraph ...
        (await fixture.TestHarness.Published.Any<MqttRequestGraph>()).ShouldBeTrue();
        // ... and the current state is StationListReceived.
        saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.StationListReceived);

        var requestIdGraph = saga.CurrentMqttRequestId; // Has been chosen by the state machine as request ID for MQTT.
        var graph = new MqttGraph(AgvSerialNumber, DateTimeOffset.UtcNow,
            new MqttNode[]
            {
                new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = "2", X = 9f, Y = 9f, Z = 0f }
            },
            new MqttEdge[]
            {
                new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1 }
            },
            string.Empty,
            requestIdGraph);
        // Publish GraphReceived ...
        await fixture.TestHarness.Bus.Publish(new InternalGraphReceived(correlationId, graph));
        // ... expect it's going to be consumed by the state machine.
        (await fixture.SagaHarness.Consumed.Any<InternalGraphReceived>()).ShouldBeTrue();

        // Check if the state machine published AgvGraphReceived finally.
        var publishedData = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;

        // Check for invalid usability due to duplicate nodes.
        publishedData.Graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidStations);
    }
}

public class AgvGraphBuilderStateMachineFixture : BaseStateMachineTestFixture<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>
{
    public AgvGraphBuilderStateMachineFixture()
    {
        Build();
    }
}