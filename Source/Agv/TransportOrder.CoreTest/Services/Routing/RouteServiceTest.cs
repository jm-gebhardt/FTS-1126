using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing;

[TestClass]
public class RouteServiceTest
{
    [TestMethod]
    public void GetRoute_WhenAgvCurrentPositionIsDestinationNode_ReturnsRoute()
    {
        var routeService = new RouteService();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.DestinationClose.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId:node, agvGraph: agvGraph);
        
        var actualRoute = routeService.GetRoute(agv, node);
       
        actualRoute.Costs.ShouldBe(0);
        actualRoute.Nodes.ShouldBe(new[] { node });
    }
}