using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Data;
using Gebhardt.Acc.DeviceCommunication.SimAgv.States;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation.Travel;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class SimulatedAgvSagaTest
{
    [TestMethod]
    public void Construct_WithSimulationSpeed_AgvDataAdjusted()
    {
        const int simulationSpeed = 2;
        var agvData = new KarisType1(Guid.NewGuid(), "ip address");
        var agv = new SimulatedAgv(agvData.AgvCorrelationId, "id", "",
            true,
            false,
            false,
            true,
            DateTime.MinValue,
            DateTime.MinValue,
            DateTime.UtcNow,
            AgvGraph: null, AgvData: agvData);
        var saga = new SimulatedAgvSaga(agv, simulationSpeed, 75, 0.1, new FakeTimeProvider());

        saga.AgvData.ShouldNotBe(agvData);
        saga.AgvData.ShouldBe(agvData.SetSimulationSpeed(simulationSpeed));
    }

    [TestMethod]
    public void Construct_WithChargerOrParker_ZoneAroundChargerOrParker()
    {
        var saga = SimulatedAgvSagaGenerator.Generate(agvGraph: AgvGraphGenerator.Generate(
                [AgvGraphGenerator.Waypoint4, AgvGraphGenerator.ChargingFar],
                [
                    AgvGraphGenerator.Edges.Single(e => e.FromNodeId == AgvGraphGenerator.Waypoint4.Id && e.ToNodeId == AgvGraphGenerator.ChargingFar.Id),
                    AgvGraphGenerator.Edges.Single(e => e.ToNodeId == AgvGraphGenerator.Waypoint4.Id && e.FromNodeId == AgvGraphGenerator.ChargingFar.Id)
                ]),
            currentNode: AgvGraphGenerator.ChargingFar);
        saga.AgvZones.ShouldNotBeNull();
        saga.AgvZones.Single().Polygon.PointIsInPolygon(AgvGraphGenerator.ChargingFar.X, AgvGraphGenerator.ChargingFar.Y).ShouldBeTrue();
    }

    [TestMethod]
    public void UpdateStatus_ChargingAgv_IncreasesBatteryLevelAndHasChargingState()
    {
        const int initialSoc = 50;
        const double chargeDiff = 0.1;
        const double timeDiff = 60;
        var now = DateTime.UtcNow;
        const double batteryChargeRate = 0.1;
        var saga = SimulatedAgvSagaGenerator.Generate(currentNode: NodeGenerator.GenerateChargingNode(),
            currentAction: AgvActionGenerator.GenerateChargeAction(),
            initialSoc: initialSoc,
            timeProvider: new FakeTimeProvider(now),
            batteryChargeRate: batteryChargeRate);
        saga.ActionStartDate = now;
        saga.UpdateStatus(timeDiff, chargeDiff, chargeDiff);

        const double batteryCharge = initialSoc + chargeDiff * timeDiff;
        saga.SimulatedBatteryCharge.ShouldBe(batteryCharge);
        saga.AgvStatus.Charging.ShouldBeTrue();
        saga.AgvStatus.EstimatedFullyChargedTime.ShouldNotBeNull();
        ((DateTime)saga.AgvStatus.EstimatedFullyChargedTime!).ShouldBe(now.AddSeconds((SimulatedAgvSaga.MaxCharge - batteryCharge) / batteryChargeRate));
    }

    [TestMethod]
    public void UpdateStatus_NotChargingAgv_DecreasesBatteryLevelAndDoesNotHaveChargingState()
    {
        const int initialSoc = 50;
        const double chargeDiff = 0.1;
        const double timeDiff = 60;
        var saga = SimulatedAgvSagaGenerator.Generate(currentNode: NodeGenerator.GenerateParkingNode(),
            initialSoc: initialSoc);

        saga.UpdateStatus(timeDiff, chargeDiff, chargeDiff);

        saga.SimulatedBatteryCharge.ShouldBe(initialSoc - chargeDiff * timeDiff);
        saga.AgvStatus.Charging.ShouldBeFalse();
    }

    [TestMethod]
    public void UpdateStatus_DyingAgv_GoesOffline()
    {
        const int initialSoc = 10;
        const double chargeDiff = 0.1;
        const double timeDiff = 60;
        var saga = SimulatedAgvSagaGenerator.Generate(currentNode: NodeGenerator.GenerateParkingNode(),
            initialSoc: initialSoc);

        saga.IsOnline.ShouldBeTrue();

        saga.UpdateStatus(timeDiff, chargeDiff, chargeDiff);

        saga.IsOnline.ShouldBeFalse();
    }

    [TestMethod]
    public void UpdateStatus_AgvIsInZone_ZoneIdInStatus()
    {
        var saga = SimulatedAgvSagaGenerator.Generate(agvGraph: AgvGraphGenerator.Generate(
                [AgvGraphGenerator.Waypoint4, AgvGraphGenerator.ChargingFar],
                [
                    AgvGraphGenerator.Edges.Single(e => e.FromNodeId == AgvGraphGenerator.Waypoint4.Id && e.ToNodeId == AgvGraphGenerator.ChargingFar.Id),
                    AgvGraphGenerator.Edges.Single(e => e.ToNodeId == AgvGraphGenerator.Waypoint4.Id && e.FromNodeId == AgvGraphGenerator.ChargingFar.Id)
                ]),
            currentNode: AgvGraphGenerator.ChargingFar);
        saga.UpdateStatus(60, 1, 1);
        saga.AgvStatus.ZoneIds.ShouldNotBeNull();
        saga.AgvStatus.ZoneIds.Single().ShouldBe(saga.AgvZones.Single().ZoneId);
    }

    [TestMethod]
    public void UpdatePosition_WithoutGraph_DoesNotThrow()
    {
        var saga = SimulatedAgvSagaGenerator.Generate(graphExists: false);
        saga.UpdatePosition();
        saga.Position.ShouldBeNull();
    }

    [TestMethod]
    public void UpdatePosition_WithGraph_InitializesRandomPosition()
    {
        var saga = SimulatedAgvSagaGenerator.Generate(agvGraph: AgvGraphGenerator.CreateDefaultGraph());
        saga.Position.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdatePosition_OnAEdge_UpdatesPositionProperly()
    {
        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 3, y: -36, nodeName: "W1"),
            NodeGenerator.GenerateNode(x: 34, y: -91, nodeName: "W2")
        };
        var fixture = new SimulatedAgvSagaTestFixture()
            .WithNumberOfEdges(1)
            .WithTheseNodes(nodes)
            .WithActionStartNodeId(nodes[0].Name)
            .WithActionTargetNodeId(nodes[1].Name)
            .Build();

        fixture.SimulateToPercentage(0);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(nodes[0].X, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(nodes[0].Y, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe(-1.0575f, 0.01);

        fixture.SimulateToPercentage(100);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(nodes[1].X, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(nodes[1].Y, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe(-1.0575f, 0.01);
    }

    [TestMethod]
    public void UpdatePosition_OnTheEndOfEdge_SetsThetaProperly()
    {
        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "W1"),
            NodeGenerator.GenerateNode(x: 10, y: 0, nodeName: "W2")
        };
        var fixture = new SimulatedAgvSagaTestFixture()
            .WithPosition(nodes[0])
            .WithNumberOfEdges(1)
            .WithTheseNodes(nodes)
            .WithActionStartNodeId(nodes[0].Name)
            .WithActionTargetNodeId(nodes[1].Name)
            .Build();

        fixture.SimulateToPercentage(100);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(10f, 0.05);
        fixture.Saga.Position!.Y.ShouldBe(0f, 0.05);
        fixture.Saga.Position!.Theta.ShouldBe(0, 0.01);
    }

    [TestMethod]
    public void UpdatePosition_CurrentNode_InitializesOnCurrentNode()
    {
        var node1 = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "W1");
        var node2 = NodeGenerator.GenerateNode(x: -10, y: 0, nodeName: "W2");

        var edge = GraphEdge.CreateDirect(node1.Id, node2.Id);
        var graph = AgvGraphGenerator.Generate(new List<GraphNode> { node1, node2 }, new List<GraphEdge> { edge });

        var fixture = new SimulatedAgvSagaTestFixture()
            .WithGraph(graph)
            .WithPosition(node1)
            .WithNumberOfEdges(0)
            .WithTheseNodes(new List<GraphNode> { node1, node2 })
            .WithActionStartNodeId(node1.Name)
            .WithActionTargetNodeId(node1.Name)
            .Build();

        fixture.Saga.Position!.X.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe((float)Math.PI, 0.01);
    }

    [TestMethod]
    public void ActionFinishDate_WithMoveAction_ConsidersPauseTime()
    {
        var saga = SimulatedAgvSagaGenerator.Generate();
        saga.ActionStartDate = new DateTime(2000, 1, 1, 0, 0, 0);
        saga.Action = AgvActionGenerator.GenerateMoveAction();
        saga.TravelSimulator = new AgvTravelSimulatorMock()
            .WithTravelDuration(new TimeSpan(0, 0, 10))
            .WithPauseTime(new TimeSpan(0, 0, 7))
            .Object;

        saga.ActionFinishDate.ShouldBe(new DateTime(2000, 1, 1, 0, 0, 17));
    }

    [TestMethod]
    public void RemainingDuration_WithActionFinishDate_CalculatesRemainingDurationProperly()
    {
        var now = new DateTime(3000, 1, 1, 0, 5, 0);
        var finish = now.AddSeconds(5);
        var fakeTimeProvider = new FakeTimeProvider(now);
        var saga = SimulatedAgvSagaGenerator.Generate(timeProvider: fakeTimeProvider);
        // As we do not have a travel duration: ActionFinishDate = ActionStartDate
        saga.ActionStartDate = finish;
        saga.Action = AgvActionGenerator.GenerateMoveAction();
        saga.TravelSimulator = new AgvTravelSimulatorMock().Object;
        var remainingDuration = (TimeSpan)saga.RemainingDuration!;

        var expectedRemaining = saga.ActionFinishDate!.Value - now;
        remainingDuration.TotalSeconds.ShouldBe(expectedRemaining.TotalSeconds);
    }

    [TestMethod]
    public void Construct_WithValidSpawnNodes_SpawnNodeIdSet()
    {
        const int simulationSpeed = 2;
        var node1 = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "W1");
        var node2 = NodeGenerator.GenerateNode(x: -10, y: 0, nodeName: "W2");

        var edge = GraphEdge.CreateDirect(node1.Id, node2.Id);
        var agvGraph = AgvGraphGenerator.Generate(new List<GraphNode> { node1, node2 }, new List<GraphEdge> { edge });

        var agvData = new KarisType1(Guid.NewGuid(), "ip address");
        var agv = new SimulatedAgv(agvData.AgvCorrelationId, "id", "",
            true,
            false,
            false,
            true,
            DateTime.MinValue,
            DateTime.MinValue,
            DateTime.UtcNow,
            AgvGraph: agvGraph, AgvData: agvData);
        var saga = new SimulatedAgvSaga(agv, simulationSpeed, 75, 0.1, new FakeTimeProvider(), null, [node2.Name]);
        saga.SpawnNodeIds.ShouldNotBeNull();
        saga.SpawnNodeIds.Length.ShouldBe(1);
        saga.SpawnNodeIds[0].ShouldBe(node2.Name);
    }

    [TestMethod]
    public void Construct_WithInvalidValidSpawnNodes_ShouldThrowArgumentException()
    {
        const int simulationSpeed = 2;
        var node1 = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "W1");
        var node2 = NodeGenerator.GenerateNode(x: -10, y: 0, nodeName: "W2");
        const string unknownNode = "Unknown";

        var edge = GraphEdge.CreateDirect(node1.Id, node2.Id);
        var agvGraph = AgvGraphGenerator.Generate(new List<GraphNode> { node1, node2 }, new List<GraphEdge> { edge });

        var agvData = new KarisType1(Guid.NewGuid(), "ip address");
        var agv = new SimulatedAgv(agvData.AgvCorrelationId, "id", "",
            true,
            false,
            false,
            true,
            DateTime.MinValue,
            DateTime.MinValue,
            DateTime.UtcNow,
            AgvGraph: agvGraph, AgvData: agvData);

        Should.Throw<ArgumentException>(() => new SimulatedAgvSaga(agv, simulationSpeed, 75, 0.1, new FakeTimeProvider(), null, [node1.Name, node2.Name, unknownNode]));
    }
}