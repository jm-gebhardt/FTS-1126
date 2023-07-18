using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing;

[TestClass]
public class RouteServiceTest
{
    [TestMethod]
    public async Task GetRoute_WhenAgvCurrentPositionIsDestinationNode_ReturnsRoute()
    {
        var agvGraphRegistry = new Mock<IAgvGraphRegistry>();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.DestinationClose.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId:node);
        agvGraphRegistry.Setup(x => x.GetAgvGraphAsync(agv.CorrelationId)).ReturnsAsync(agvGraph);
        agvGraphRegistry.Setup(x => x.ExistsAsync(agv.CorrelationId)).ReturnsAsync(true);
        var routeService = new RouteService(agvGraphRegistry.Object);
        
        var actualRoute = await routeService.GetRouteAsync(agv, node);
  
        actualRoute.Costs.ShouldBe(0);
        actualRoute.Nodes.ShouldBe(new[] { node });
    }
}