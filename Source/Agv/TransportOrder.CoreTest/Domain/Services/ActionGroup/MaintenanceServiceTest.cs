using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

[TestClass]
public class MaintenanceServiceTest
{
    [TestMethod]
    public async Task GetMaintenanceNode_WhenThereMoreThanOneAvailableMaintenanceNodes_ReturnsLowestCostNode()
    {
        var availableNodes = new List<GraphNode>
        {
            NodeGenerator.GenerateParkingNode("Park-1"),
            NodeGenerator.GenerateParkingNode("Park-2")
        };

        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new MaintenanceServiceTestFixture()
            .WithAvailableNodes(agv, availableNodes)
            .WithLowestCostNode(agv, availableNodes, availableNodes.First());

        var maintenanceNode = await fixture.MaintenanceService.GetMaintenanceNode(agv);
        fixture.RouteService.Verify(m => m.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>(), null), Times.Once);
        maintenanceNode.ShouldNotBeNull();
        maintenanceNode.Id.ShouldBe("Park-1");
        maintenanceNode.Type.ShouldBe(AgvGraph.NodeType.Park);
    }
}