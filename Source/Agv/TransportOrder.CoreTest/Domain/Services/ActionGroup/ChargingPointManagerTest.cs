using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

[TestClass]
public class ChargingPointManagerTest
{
    private static void VerifyGraphNode(GraphNode actualNode, GraphNode expectedNode)
    {
        actualNode.ShouldNotBeNull();
        actualNode.ShouldSatisfyAllConditions(
            n => n.Id.ShouldBe(expectedNode.Id),
            n => n.Type.ToString().ShouldBe(expectedNode.Type.ToString()),
            n => n.X.ShouldBe(expectedNode.X),
            n => n.Y.ShouldBe(expectedNode.Y),
            n => n.Z.ShouldBe(expectedNode.Z),
            n => n.Theta.ShouldBe(expectedNode.Theta));
    }

    [TestMethod]
    public async Task GetBestCharger_WhenFreeChargerNodesExists_ReturnFirstFreeChargerNode()
    {
        const string agvSerialNumber = "AGV001";
        var correlationId = agvSerialNumber.ToGuid();
        var fixture = new ChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);
        var agv = fixture.GetAgv(correlationId, agvSerialNumber, "Idle", "");
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };

        fixture.SetupGraphRepository(agvGraph);
        fixture.SetupNodeLocker(true, true, true);
        fixture.WithAvailableNodes(agv, availableNodes)
            .WithLowestCostNode(agv, availableNodes, chargingNode);

        var bestCharger = await fixture.CreateChargingPointManager().GetBestChargerAsync(agv);
        bestCharger.ShouldNotBeNull();
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        VerifyGraphNode(bestCharger, chargingNode);
    }

    [TestMethod]
    public async Task GetBestCharger_WhenFreeChargerNodesDoesNotExists_ReturnNull()
    {
        const string agvSerialNumber = "AGV001";
        var correlationId = agvSerialNumber.ToGuid();
        var fixture = new ChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);
        var agv = fixture.GetAgv(correlationId, agvSerialNumber, "Idle", "");

        fixture.SetupGraphRepository(agvGraph);
        fixture.SetupNodeLocker(true, true, true);

        var bestCharger = await fixture.CreateChargingPointManager().GetBestChargerAsync(agv);
        bestCharger.ShouldBeNull();
    }

    [TestMethod]
    public async Task ReserveCharger_WhenNodeIsChargerTypeAndFree_ReturnTrue()
    {
        const string agvSerialNumber = "AGV001";
        var correlationId = agvSerialNumber.ToGuid();
        var fixture = new ChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);
        var agv = fixture.GetAgv(correlationId, agvSerialNumber, "Idle", "");
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };

        fixture.SetupGraphRepository(agvGraph);
        fixture.SetupNodeLocker(true, true, true);
        fixture.WithAvailableNodes(agv, availableNodes)
            .WithLowestCostNode(agv, availableNodes, chargingNode);

        var baseChargingPointManager = fixture.CreateChargingPointManager();
        var bestCharger = await baseChargingPointManager.GetBestChargerAsync(agv);
        bestCharger.ShouldNotBeNull();
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);

        await baseChargingPointManager.ReserveChargerAsync(bestCharger, correlationId);
        fixture.NodeStateRepository.Verify(x => x.ReserveNodeAsync(bestCharger.Id, correlationId), Times.Once);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        VerifyGraphNode(bestCharger, chargingNode);
    }

    [TestMethod]
    public async Task ReserveCharger_WhenNodeIsNotChargerTypeAndFree_ReturnFalse()
    {
        var correlationId = "AGV001".ToGuid();
        var fixture = new ChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);

        var notChargerNode = agvGraph.Nodes.FirstOrDefault(node => node.Type != AgvGraph.NodeType.Charge);
        notChargerNode.ShouldNotBeNull();
        notChargerNode.Type.ShouldNotBe(AgvGraph.NodeType.Charge);

        fixture.SetupGraphRepository(agvGraph);
        fixture.SetupNodeLocker(true, true, true);

        await fixture.CreateChargingPointManager().ReserveChargerAsync(notChargerNode, correlationId);
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenFreeChargerExist_ReturnChargerNodeInReservedState()
    {
        const string agvSerialNumber = "AGV001";
        var correlationId = agvSerialNumber.ToGuid();

        var fixture = new ChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);
        var agv = fixture.GetAgv(correlationId, agvSerialNumber, "Idle", "");
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };

        fixture.SetupGraphRepository(agvGraph);
        fixture.SetupNodeLocker(true, true, true);
        fixture.WithAvailableNodes(agv, availableNodes)
            .WithLowestCostNode(agv, availableNodes, chargingNode);

        var agvNodeResult = await fixture.CreateChargingPointManager().ReserveChargerAsync(agv);
        agvNodeResult.ShouldNotBeNull();
        fixture.NodeStateRepository.Verify(r => r.ReserveNodeAsync(agvNodeResult.Id, correlationId), Times.Once);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        agvNodeResult.Type.ShouldBe(AgvGraph.NodeType.Charge);
        VerifyGraphNode(agvNodeResult, chargingNode);
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenNoChargerExist_ThrowsException()
    {
        const string agvSerialNumber = "AGV001";
        var correlationId = agvSerialNumber.ToGuid();
        var fixture = new ChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);
        var agv = fixture.GetAgv(correlationId, agvSerialNumber, "Idle", "");

        fixture.SetupGraphRepository(agvGraph);
        fixture.SetupNodeLocker(true, true, true);

        await Should.ThrowAsync<NoChargeStationAvailableException>(async () => await fixture.CreateChargingPointManager().ReserveChargerAsync(agv));
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenReserveFails_ThrowsException()
    {
        const string agvSerialNumber = "AGV001";
        var correlationId = agvSerialNumber.ToGuid();

        var fixture = new ChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);
        var agv = fixture.GetAgv(correlationId, agvSerialNumber, "Idle", "");
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };

        fixture.SetupGraphRepository(agvGraph);
        fixture.WithAvailableNodes(agv, availableNodes)
            .WithLowestCostNode(agv, availableNodes, chargingNode);
        fixture.SetupNodeLocker(false, false, false);

        await Should.ThrowAsync<NoChargeStationAvailableException>(async () => await fixture.CreateChargingPointManager().ReserveChargerAsync(agv));
    }

    [TestMethod]
    public async Task ReserveChargerAsync_WhenReserveFails_ThrowsException()
    {
        const string agvSerialNumber = "AGV001";
        var correlationId = agvSerialNumber.ToGuid();

        var fixture = new ChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);
        var agv = fixture.GetAgv(correlationId, agvSerialNumber, "Idle", "");
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };

        fixture.SetupGraphRepository(agvGraph);
        fixture.WithAvailableNodes(agv, availableNodes)
            .WithLowestCostNode(agv, availableNodes, chargingNode);
        fixture.SetupNodeLocker(false, false, false);

        await Should.ThrowAsync<NodeNotAvailableException>(async () => await fixture.CreateChargingPointManager().ReserveChargerAsync(chargingNode, correlationId));
    }

    [TestMethod]
    public async Task GetBestChargerAsync_WhenMoreThanOneAvailableChargerNodes_ReturnsNearestAvailableNode()
    {
        var availableNodes = new List<GraphNode>
        {
            NodeGenerator.GenerateChargingNode("Charge-1"),
            NodeGenerator.GenerateChargingNode("Charge-2")
        };

        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ChargingPointManagerTestFixture()
            .WithAvailableNodes(agv, availableNodes)
            .WithLowestCostNode(agv, availableNodes, availableNodes.Last());

        var bestCharger = await fixture.CreateChargingPointManager().GetBestChargerAsync(agv);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        bestCharger.ShouldNotBeNull();
        bestCharger.Id.ShouldBe("Charge-2");
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);
    }

    [TestMethod]
    public async Task ReserveChargerAsync_WhenMoreThanOneAvailableChargerNodes_ReserveNearestAvailableNode()
    {
        var availableNodes = new List<GraphNode>
        {
            NodeGenerator.GenerateChargingNode("Charge-1"),
            NodeGenerator.GenerateChargingNode("Charge-2")
        };

        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ChargingPointManagerTestFixture()
            .WithAvailableNodes(agv, availableNodes)
            .WithLowestCostNode(agv, availableNodes, availableNodes.Last());

        fixture.SetupNodeLocker(true, true, true);

        var reservedCharger = await fixture.CreateChargingPointManager().ReserveChargerAsync(agv);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        reservedCharger.ShouldNotBeNull();
        reservedCharger.Id.ShouldBe("Charge-2");
        reservedCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);
    }
}