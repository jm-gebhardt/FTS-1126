using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

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
        maintenanceNode.Name.ShouldBe("Park-1");
        maintenanceNode.Type.ShouldBe(AgvGraph.NodeType.Park);
    }
}