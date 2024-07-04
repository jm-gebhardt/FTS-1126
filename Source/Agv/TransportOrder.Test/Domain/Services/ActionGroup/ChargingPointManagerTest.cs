using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

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
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };
        
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, chargingNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(true, true, true);

        var bestCharger = await fixture.ChargingPointManager.GetBestChargerAsync(fixture.Agv);
        bestCharger.ShouldNotBeNull();
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(fixture.Agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        VerifyGraphNode(bestCharger, chargingNode);
    }

    [TestMethod]
    public async Task GetBestCharger_WhenFreeChargerNodesDoesNotExists_ReturnNull()
    {
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithChargingPointManager()
            .WithGraphRepository(false)
            .WithNodeLocker(true, true, true);

        var bestCharger = await fixture.ChargingPointManager.GetBestChargerAsync(fixture.Agv);
        bestCharger.ShouldBeNull();
    }

    [TestMethod]
    public async Task ReserveCharger_WhenNodeIsChargerTypeAndFree_ReturnTrue()
    {
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };
        
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, chargingNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(true, true, true);
        
        var bestCharger = await fixture.ChargingPointManager.GetBestChargerAsync(fixture.Agv);
        bestCharger.ShouldNotBeNull();
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);

        var correlationId = ChargingPointManagerTestFixture.AgvSerialNumber.ToGuid();
        await fixture.ChargingPointManager.ReserveChargerAsync(bestCharger, correlationId);
        fixture.NodeStateRepository.Verify(x => x.ReserveNodeAsync(bestCharger.Id, correlationId), Times.Once);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(fixture.Agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        VerifyGraphNode(bestCharger, chargingNode);
    }

    [TestMethod]
    public async Task ReserveCharger_WhenNodeIsNotChargerTypeAndFree_ReturnFalse()
    {
        var fixture = new ChargingPointManagerTestFixture()
            .WithChargingPointManager()
            .WithGraphRepository(false)
            .WithNodeLocker(true, true, true);

        var notChargerNode = fixture.AgvGraph.Nodes.FirstOrDefault(node => node.Type != AgvGraph.NodeType.Charge);
        notChargerNode.ShouldNotBeNull();
        notChargerNode.Type.ShouldNotBe(AgvGraph.NodeType.Charge);

        await fixture.ChargingPointManager.ReserveChargerAsync(notChargerNode, ChargingPointManagerTestFixture.AgvSerialNumber.ToGuid());
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenFreeChargerExist_ReturnChargerNodeInReservedState()
    {
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };
        
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, chargingNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(true, true, true);

        var agvNodeResult = await fixture.ChargingPointManager.ReserveChargerAsync(fixture.Agv);
        agvNodeResult.ShouldNotBeNull();
        fixture.NodeStateRepository.Verify(r => r.ReserveNodeAsync(agvNodeResult.Id, ChargingPointManagerTestFixture.AgvSerialNumber.ToGuid()), Times.Once);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(fixture.Agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        agvNodeResult.Type.ShouldBe(AgvGraph.NodeType.Charge);
        VerifyGraphNode(agvNodeResult, chargingNode);
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenNoChargerExist_ThrowsException()
    {
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithChargingPointManager()
            .WithGraphRepository(false)
            .WithNodeLocker(true, true, true);
       
        await Should.ThrowAsync<NoChargeStationAvailableException>(async () => await fixture.ChargingPointManager.ReserveChargerAsync(fixture.Agv));
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenReserveFails_ThrowsException()
    {
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };
        
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, chargingNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(false, false, false);

        await Should.ThrowAsync<NoChargeStationAvailableException>(async () => await fixture.ChargingPointManager.ReserveChargerAsync(fixture.Agv));
    }

    [TestMethod]
    public async Task ReserveChargerAsync_WhenReserveFails_ThrowsException()
    {
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var availableNodes = new List<GraphNode>
        {
            chargingNode
        };
        
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithGraphRepository(true)
            .WithChargingPointManager()
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, chargingNode)
            .WithNodeLocker(false, false, false);
        
        await Should.ThrowAsync<NodeNotAvailableException>(async () => await fixture.ChargingPointManager.ReserveChargerAsync(chargingNode, fixture.Agv.CorrelationId));
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
            .WithAgv(agv)
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, availableNodes.Last())
            .WithChargingPointManager();

        var bestCharger = await fixture.ChargingPointManager.GetBestChargerAsync(agv);
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
            .WithAgv(agv)
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, availableNodes.Last())
            .WithChargingPointManager()
            .WithNodeLocker(true, true, true);
        
        var reservedCharger = await fixture.ChargingPointManager.ReserveChargerAsync(agv);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        reservedCharger.ShouldNotBeNull();
        reservedCharger.Id.ShouldBe("Charge-2");
        reservedCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);
    }
}