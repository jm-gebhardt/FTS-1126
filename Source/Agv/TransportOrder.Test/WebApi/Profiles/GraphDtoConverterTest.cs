using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using Fixture = Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles.GraphDtoConverterTestFixture;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class GraphDtoConverterTest
{
    [TestMethod]
    public void Convert_ValidData_ListsEachAgvStateOnceOrderedByAgvTypeAndLoadState()
    {
        var graph = new Fixture().Map();

        graph.AgvStates.Select(s => (s.AgvTypeId, s.LoadState.Id, s.LoadState.IsLoaded)).ShouldBe(
        [
            (NewGraphGenerator.AgvTypeAId, NewGraphGenerator.AgvTypeAUnloadedId, false),
            (NewGraphGenerator.AgvTypeAId, NewGraphGenerator.AgvTypeALoadedId, true),
            (NewGraphGenerator.AgvTypeBId, NewGraphGenerator.AgvTypeBUnloadedId, false)
        ]);
    }

    [TestMethod]
    public void Convert_ValidData_NodeReferencesTheStatesOfItsRoutingNodes()
    {
        var graph = new Fixture().Map();

        Fixture.GetNode(graph, NewGraphGenerator.StartNodeName).AgvStateIndices.ShouldBe([0, 1, 2]);
    }

    [TestMethod]
    public void Convert_ValidData_EdgeGroupsRoutingEdgesByTrajectory()
    {
        var graph = new Fixture().Map();

        var edge = Fixture.GetEdge(graph, NewGraphGenerator.EdgeName);
        edge.AgvStateIndices.ShouldBe([0, 1, 2]);
        edge.Trajectories.Count.ShouldBe(2);
        var curve = edge.Trajectories[0];
        curve.AgvStateIndices.ShouldBe([0, 1]);
        curve.ControlPoints.Count.ShouldBe(3);
        curve.Degree.ShouldBe(2);
        var straight = edge.Trajectories[1];
        straight.AgvStateIndices.ShouldBe([2]);
        straight.ControlPoints.ShouldBeEmpty();
        straight.KnotVector.ShouldBeEmpty();
        straight.Degree.ShouldBeNull();
    }

    [TestMethod]
    public void Convert_EdgeWithoutRoutingEdges_HasNoStatesAndNoTrajectories()
    {
        var graph = new Fixture().Map();

        var backEdge = Fixture.GetEdge(graph, NewGraphGenerator.BackEdgeName);
        backEdge.AgvStateIndices.ShouldBeEmpty();
        backEdge.Trajectories.ShouldBeEmpty();
    }

    [TestMethod]
    public void Convert_RoutingNodeActions_BecomeNodeAndStationCapabilities()
    {
        var graph = new Fixture().Map();

        Fixture.GetNode(graph, NewGraphGenerator.StartNodeName).Capabilities.ShouldBe([RoutingActionType.Pick, RoutingActionType.Drop]);
        Fixture.GetNode(graph, NewGraphGenerator.EndNodeName).Capabilities.ShouldBeEmpty();
        graph.Stations.ShouldHaveSingleItem().Capabilities.ShouldBe([RoutingActionType.Pick, RoutingActionType.Drop]);
    }

    [TestMethod]
    public void Convert_ValidData_NodeStatusIsFreeWithoutLock()
    {
        var graph = new Fixture().Map();

        graph.Nodes.ShouldAllBe(n => n.Status.Occupancy == OccupancyState.Free && n.Status.AgvId == null && n.Status.Lock == null);
    }

    [TestMethod]
    public void Convert_ValidData_StationStatusIsUnlockedWithoutPalletJam()
    {
        var graph = new Fixture().Map();

        var status = graph.Stations.ShouldHaveSingleItem().Status;
        status.Lock.ShouldBeNull();
        status.LockedOrderHandling.ShouldBeNull();
        status.HasPalletJam.ShouldBeFalse();
        status.IsOnline.ShouldBeNull();
    }
}