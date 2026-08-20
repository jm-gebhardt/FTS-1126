using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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
            n => n.Name.ShouldBe(expectedNode.Name),
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
            .WithNode(chargingNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(true);

        var bestCharger = await fixture.ChargingPointManager.GetBestChargerAsync(fixture.Agv);
        bestCharger.ShouldNotBeNull();
        bestCharger.Type.First().ShouldBe(AgvGraph.NodeType.Charge);
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
            .WithNodeLocker(true);

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
            .WithNode(chargingNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(true);

        var bestCharger = await fixture.ChargingPointManager.GetBestChargerAsync(fixture.Agv);
        bestCharger.ShouldNotBeNull();
        bestCharger.Type.First().ShouldBe(AgvGraph.NodeType.Charge);

        var correlationId = ChargingPointManagerTestFixture.AgvSerialNumber.ToGuid();
        await fixture.ChargingPointManager.ReserveChargerAsync(bestCharger, correlationId);
        fixture.NodeRepository.Verify(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(fixture.Agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        VerifyGraphNode(bestCharger, chargingNode);
    }

    [TestMethod]
    public async Task ReserveCharger_WhenNodeIsNotChargerTypeAndFree_ReturnFalse()
    {
        var fixture = new ChargingPointManagerTestFixture()
            .WithChargingPointManager()
            .WithGraphRepository(false)
            .WithNodeLocker(true);

        var notChargerNode = fixture.AgvGraph.Nodes.FirstOrDefault(node => !node.IsCharger());
        notChargerNode.ShouldNotBeNull();
        notChargerNode.Type.First().ShouldNotBe(AgvGraph.NodeType.Charge);
        fixture.WithNode(notChargerNode);

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
            .WithNode(chargingNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(true);

        var agvNodeResult = await fixture.ChargingPointManager.ReserveChargerAsync(fixture.Agv);
        agvNodeResult.ShouldNotBeNull();
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(fixture.Agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        agvNodeResult.Type.First().ShouldBe(AgvGraph.NodeType.Charge);
        agvNodeResult.Lock.ShouldNotBeNull();
        agvNodeResult.Lock.NodeState.ShouldBe(NodeLockState.Reserved);
        VerifyGraphNode(agvNodeResult, chargingNode);
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenRepositoryReturnsDifferentInstance_ReturnUpdatedReservedNode()
    {
        var selectedNode = NodeGenerator.GenerateChargingNode();
        var repositoryNode = new GraphNode(selectedNode.Id, selectedNode.Name, selectedNode.X, selectedNode.Y, selectedNode.Z, selectedNode.Type.ToList(), selectedNode.PickLiftHeight, selectedNode.DropLiftHeight, selectedNode.DockingDirection);
        var availableNodes = new List<GraphNode>
        {
            selectedNode
        };

        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, selectedNode)
            .WithNode(repositoryNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(true);

        var reservedNode = await fixture.ChargingPointManager.ReserveChargerAsync(fixture.Agv);

        reservedNode.ShouldNotBeSameAs(selectedNode);
        reservedNode.Lock.ShouldNotBeNull();
        reservedNode.Lock.NodeState.ShouldBe(NodeLockState.Reserved);
        reservedNode.Lock.AgvCorrelationId.ShouldBe(fixture.Agv.CorrelationId);
        selectedNode.Lock.ShouldBeNull();
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenNoChargerExist_ThrowsException()
    {
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv()
            .WithChargingPointManager()
            .WithGraphRepository(false)
            .WithNodeLocker(true);

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
            .WithNode(chargingNode)
            .WithChargingPointManager()
            .WithGraphRepository(true)
            .WithNodeLocker(false);

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
            .WithNode(chargingNode)
            .WithNodeLocker(false);

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
            .WithNode(availableNodes.Last())
            .WithChargingPointManager();

        var bestCharger = await fixture.ChargingPointManager.GetBestChargerAsync(agv);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        bestCharger.ShouldNotBeNull();
        bestCharger.Name.ShouldBe("Charge-2");
        bestCharger.Type.First().ShouldBe(AgvGraph.NodeType.Charge);
    }

    [TestMethod]
    public async Task ReserveChargerAsync_WhenOnlyChargerIsLocked_ThrowsWithoutSelectingNode()
    {
        var lockedCharger = ChargingPointManagerTestFixture.GenerateLockedChargingNode();
        var availableNodes = new List<GraphNode>
        {
            lockedCharger
        };

        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv(agv)
            .WithAvailableNodes(availableNodes)
            .WithNode(lockedCharger)
            .WithChargingPointManager()
            .WithNodeLocker(true);

        await Should.ThrowAsync<NoChargeStationAvailableException>(async () => await fixture.ChargingPointManager.ReserveChargerAsync(agv));
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<IList<GraphNode>>(), It.IsAny<AgvData>(), It.IsAny<Guid?>()), Times.Never);
    }

    [TestMethod]
    public async Task ReserveChargerAsync_WhenChargerIsLockedAndIgnoreLocked_ReservesLockedCharger()
    {
        var lockedCharger = ChargingPointManagerTestFixture.GenerateLockedChargingNode();
        var availableNodes = new List<GraphNode>
        {
            lockedCharger
        };

        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ChargingPointManagerTestFixture()
            .WithAgv(agv)
            .WithAvailableNodes(availableNodes)
            .WithLowestCostNode(availableNodes, lockedCharger)
            .WithNode(lockedCharger)
            .WithChargingPointManager()
            .WithNodeLocker(true);

        var reservedCharger = await fixture.ChargingPointManager.ReserveChargerAsync(agv, ignoreLocked: true);
        reservedCharger.ShouldNotBeNull();
        reservedCharger.Name.ShouldBe(ChargingPointManagerTestFixture.LockedChargerName);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
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
            .WithNode(availableNodes.Last())
            .WithChargingPointManager()
            .WithNodeLocker(true);

        var reservedCharger = await fixture.ChargingPointManager.ReserveChargerAsync(agv);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        reservedCharger.ShouldNotBeNull();
        reservedCharger.Name.ShouldBe("Charge-2");
        reservedCharger.Type.First().ShouldBe(AgvGraph.NodeType.Charge);
    }
}