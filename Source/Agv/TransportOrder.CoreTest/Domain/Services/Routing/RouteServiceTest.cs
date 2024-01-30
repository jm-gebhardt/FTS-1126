using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.Routing;

[TestClass]
public class RouteServiceTest
{
    [TestMethod]
    public async Task GetRoute_WhenAgvCurrentPositionIsDestinationNode_ReturnsRoute()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.DestinationClose.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetRouteAsync(agv, node);

        actualRoute.Costs.ShouldBe(0);
        actualRoute.Nodes.ShouldBe(new[] { node });
    }

    [TestMethod]
    public async Task GetRoute_WhenLastNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string destinationNode = agvGraph.Nodes.Skip(5).First().Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "unknown");
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, destinationNode));
    }

    [TestMethod]
    public async Task GetRoute_WhenStartNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string destinationNode = agvGraph.Nodes.Skip(5).First().Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: agvGraph.Nodes.First().Id);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, destinationNode, "unknown"));
    }

    [TestMethod]
    public async Task GetRoute_WhenDestinationNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: agvGraph.Nodes.First().Id);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, "unknown"));
    }

    [TestMethod]
    public async Task GetNodeWithLowestCostsAsync_MultipleNodes_ReturnsNodeWithLowestCosts()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.Waypoint1.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetNodeWithLowestCostsAsync(agv, AgvGraphGenerator.Nodes, It.IsAny<AgvData>());

        actualRoute.Id.ShouldBe(AgvGraphGenerator.Waypoint1.Id);
    }

    [TestMethod]
    public async Task GetNodeWithLowestCostsAsync_NoNodeReachable_Throws()
    {
        string node = AgvGraphGenerator.DestinationUnreachable.Id;
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(AgvGraphGenerator.Nodes.Where(n => n.Id != node).ToArray());
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<RouteNotFoundException>(async () => await fixture.RouteService.GetNodeWithLowestCostsAsync(agv, AgvGraphGenerator.Nodes, It.IsAny<AgvData>()));
    }
}