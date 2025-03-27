using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

[TestClass]
public class TransportOrderStationRepositoryTest
{
    private static Mapper Mapper => MappingHelper.GetMapper(new GraphProfile());
    
    [TestMethod]
    public async Task GetStationAtLocationOfNode_NodeIsStation_ReturnsStationName()
    {
        var fixture = new TransportOrderStationRepositoryTestFixture();
        var stationNode = new GraphNode("nodeId", 1, 1, 1, AgvGraph.NodeType.PickDrop);
        var efNode = Mapper.Map<EfNode>(stationNode);
        await fixture.AddAsync(efNode);

        (await fixture.Repository.GetStationAtLocationOfNode(stationNode.Id)).ShouldBe(stationNode.Id);
    }

    [TestMethod]
    public async Task GetStationAtLocationOfNode_NodeIsStationWithMultipleTypes_ReturnsStationName()
    {
        var fixture = new TransportOrderStationRepositoryTestFixture();
        var stationNode = new GraphNode("nodeId", 1, 1, 1, [AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park]);
        var efNode = Mapper.Map<EfNode>(stationNode);
        await fixture.AddAsync(efNode);

        (await fixture.Repository.GetStationAtLocationOfNode(stationNode.Id)).ShouldBe(stationNode.Id);
    }

    [TestMethod]
    public async Task GetStationAtLocationOfNode_NodeIsWaypoint_ReturnsNull()
    {
        var fixture = new TransportOrderStationRepositoryTestFixture();
        var stationNode = new GraphNode("nodeId", 1, 1, 1, AgvGraph.NodeType.WayPoint);
        var efNode = Mapper.Map<EfNode>(stationNode);
        await fixture.AddAsync(efNode);

        (await fixture.Repository.GetStationAtLocationOfNode(stationNode.Id)).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetStationAtLocationOfNode_NodeIsLocatedAtWaypointAndStation_ReturnsStationName()
    {
        var fixture = new TransportOrderStationRepositoryTestFixture();
        var stationNode = new GraphNode("AP1", -13.2223f, -2.8f, 0, AgvGraph.NodeType.PickDrop);
        var efStationNode = Mapper.Map<EfNode>(stationNode);
        var waypointNode = new GraphNode("nodeId", stationNode.X, stationNode.Y, stationNode.Z, AgvGraph.NodeType.WayPoint);
        var efWaypointNode = Mapper.Map<EfNode>(waypointNode);

        await fixture.AddRangeAsync(efWaypointNode, efStationNode);

        (await fixture.Repository.GetStationAtLocationOfNode(waypointNode.Id)).ShouldBe(stationNode.Id);
    }
}