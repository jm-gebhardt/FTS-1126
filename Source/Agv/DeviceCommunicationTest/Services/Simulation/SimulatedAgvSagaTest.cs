using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class SimulatedAgvSagaTest
{
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
    public void UpdatePosition_MultipleEdges_UpdatesCurrentNodeProperly()
    {
        var fixture = new SimulatedAgvSagaTestFixture()
            .WithNumberOfEdges(10)
            .Build();

        // simulate through whole action. For rounding reasons, just simulate with 101 steps
        for (decimal progressPercentage = 0; progressPercentage < 100; progressPercentage += (decimal) 101 / 100)
        {
            fixture.SimulateToPercentage(progressPercentage);
            fixture.Saga.UpdatePosition();

            var currentNode = fixture.Saga.CurrentNode;
            currentNode.ShouldNotBeNull();
            int index = (int) (progressPercentage / fixture.NumberOfEdges);
            currentNode.Id.ShouldBe(fixture.Action.Nodes.ElementAt(index));
        }
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

        fixture.SimulateToPercentage(1);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(3.31f, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(36.55f, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe(2.63f, 0.01);

        fixture.SimulateToPercentage(50);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(18.485f, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(63.476f, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe(2.63f, 0.01);

        fixture.SimulateToPercentage(99);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(33.66f, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(90.40f, 0.01);
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
        fixture.Saga.Position!.X.ShouldBe(10f, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe((float) Math.PI / 2, 0.01);
    }

    [TestMethod]
    public void UpdatePosition_CurrentNode_InitializesOnCurrentNode()
    {
        var node1 = NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "W1");
        var node2 = NodeGenerator.GenerateNode(x: -10, y: 0, nodeId: "W2");

        var edge = GraphEdge.CreateDirect(node1.Id, node2.Id);
        var graph = AgvGraphGenerator.Generate(new List<GraphNode> {node1, node2}, new List<GraphEdge> {edge});

        var fixture = new SimulatedAgvSagaTestFixture()
            .WithGraph(graph)
            .WithPosition(node1)
            .WithNumberOfEdges(0)
            .WithActionStartNodeId(node1.Id)
            .WithActionTargetNodeId(node1.Id)
            .Build();

        fixture.Saga.Position!.X.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe((float) 3 / 2 * (float) Math.PI, 0.01);
    }

    [TestMethod]
    public void UpdatePosition_AlreadyOnTheTargetNode_UsesPreviousTheta()
    {
        var node1 = NodeGenerator.GenerateNode(x: 0, y: 0, nodeId: "W1");
        var node2 = NodeGenerator.GenerateNode(x: 10, y: 0, nodeId: "W2");

        var edge = GraphEdge.CreateDirect(node1.Id, node2.Id);
        var graph = AgvGraphGenerator.Generate(new List<GraphNode> {node1, node2}, new List<GraphEdge> {edge});

        var fixture = new SimulatedAgvSagaTestFixture()
            .WithGraph(graph)
            .WithPosition(node1)
            .WithNumberOfEdges(0)
            .WithActionStartNodeId(node1.Id)
            .WithActionTargetNodeId(node1.Id)
            .Build();

        fixture.SimulateToPercentage(50);
        fixture.Saga.UpdatePosition();
        fixture.Saga.Position!.X.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Y.ShouldBe(0f, 0.01);
        fixture.Saga.Position!.Theta.ShouldBe((float) Math.PI / 2, 0.01);
    }
}