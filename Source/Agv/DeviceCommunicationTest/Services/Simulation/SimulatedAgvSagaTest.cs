using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Time;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Data;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class SimulatedAgvSagaTest
{
    [TestMethod]
    public void Construct_WithSimulationSpeed_AgvDataAdjusted()
    {
        const int simulationSpeed = 2;
        var agvData = new KarisType1(Guid.NewGuid(), "ip address");
        var agv = new SimulatedAgv(agvData.CorrelationId, "id", "",
            true,
            false,
            false,
            true,
            DateTime.MinValue,
            DateTime.MinValue,
            AgvGraph: null, AgvData: agvData);
        var saga = new SimulatedAgvSaga(agv, simulationSpeed, 75, 0.1, new DateTimeNowUtcProvider());

        saga.AgvData.ShouldNotBe(agvData);
        saga.AgvData.ShouldBe(agvData.SetSimulationSpeed(simulationSpeed));
    }

    [TestMethod]
    public void UpdateStatus_ChargingAgv_IncreasesBatteryLevelAndHasChargingState()
    {
        const int initialSoc = 50;
        const double chargeDiff = 0.1;
        const double timeDiff = 60;
        var saga = SimulatedAgvSagaGenerator.Generate(currentNode: NodeGenerator.GenerateChargingNode(),
            currentAction: AgvActionGenerator.GenerateChargeAction(),
            initialSoc: initialSoc);
        saga.ActionStartDate = DateTime.UtcNow;
        saga.UpdateStatus(timeDiff, chargeDiff, chargeDiff);

        saga.SimulatedBatteryCharge.ShouldBe(initialSoc + chargeDiff * timeDiff);
        saga.AgvStatus.Charging.ShouldBeTrue();
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
            NodeGenerator.GenerateNode(x: 3, y: 36, nodeId: "W1"),
            NodeGenerator.GenerateNode(x: 34, y: 91, nodeId: "W2")
        };
        var fixture = new SimulatedAgvSagaTestFixture()
            .WithNumberOfEdges(1)
            .WithTheseNodes(nodes)
            .WithActionStartNodeId(nodes[0].Id)
            .WithActionTargetNodeId(nodes[1].Id)
            .Build();

        fixture.SimulateToPercentage(0);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(nodes[0].X, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(nodes[0].Y, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe(2.63f, 0.01);

        fixture.SimulateToPercentage(100);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(nodes[1].X, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(nodes[1].Y, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe(2.63f, 0.01);
    }

    [TestMethod]
    public void UpdatePosition_OnTheEndOfEdge_SetsThetaProperly()
    {
        var nodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "W1"),
            NodeGenerator.GenerateNode(x: 10, y: 0, nodeId: "W2")
        };
        var fixture = new SimulatedAgvSagaTestFixture()
            .WithPosition(nodes[0])
            .WithNumberOfEdges(1)
            .WithTheseNodes(nodes)
            .WithActionStartNodeId(nodes[0].Id)
            .WithActionTargetNodeId(nodes[1].Id)
            .Build();

        fixture.SimulateToPercentage(100);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(10f, 0.05);
        fixture.Saga.Position!.Y.ShouldBe(0f, 0.05);
        fixture.Saga.Position!.Theta.ShouldBe((float)Math.PI / 2, 0.01);
    }

    [TestMethod]
    public void UpdatePosition_CurrentNode_InitializesOnCurrentNode()
    {
        var node1 = NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "W1");
        var node2 = NodeGenerator.GenerateNode(x: -10, y: 0, nodeId: "W2");

        var edge = GraphEdge.CreateDirect(node1.Id, node2.Id);
        var graph = AgvGraphGenerator.Generate(new List<GraphNode> { node1, node2 }, new List<GraphEdge> { edge });

        var fixture = new SimulatedAgvSagaTestFixture()
            .WithGraph(graph)
            .WithPosition(node1)
            .WithNumberOfEdges(0)
            .WithTheseNodes(new List<GraphNode> { node1, node2 })
            .WithActionStartNodeId(node1.Id)
            .WithActionTargetNodeId(node1.Id)
            .Build();

        fixture.Saga.Position!.X.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe((float)3 / 2 * (float)Math.PI, 0.01);
    }
}