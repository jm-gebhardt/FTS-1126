using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing;

[TestClass]
public class RouteServiceTest
{
    [TestMethod]
    public void GetRoute_WhenAgvCurrentPositionIsDestinationNode_ReturnsRoute()
    {
        var routeService = new RouteService();
        var graphFixture = new Mock<AgvGraphTestFixture>().Object;
        var agvGraph = graphFixture.Graph;
        string node = agvGraph.Nodes.First().Id; 
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId:node, agvGraph: agvGraph);
        
        var actualRoute = routeService.GetRoute(agv, node);
       
        actualRoute.Costs.ShouldBe(0);
        actualRoute.Nodes.ShouldBe(new[] { node });
    }
}