using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Models;

[TestClass]
public class RouteTest
{
    [TestMethod]
    public void Release_ValidData_AllNodesAndEdgesReleased()
    {
        var route = RouteGenerator.GenerateFromNodeIds(["1", "2"]);
        route.Nodes.All(n => n.Released).ShouldBeFalse();
        route.Edges.All(n => n.Released).ShouldBeFalse();

        route.Release();

        route.Nodes.All(n => n.Released).ShouldBeTrue();
        route.Edges.All(n => n.Released).ShouldBeTrue();
    }

    [TestMethod]
    public void ShiftSequenceIds_ValidData_SequenceIdsShifted()
    {
        const int offset = 2;
        var route = RouteGenerator.GenerateFromNodeIds(["1", "2"]);
        route.Nodes.Select(n => n.SequenceId).ToList().ShouldBeEquivalentTo(new List<int> { 0, 2 });
        route.Edges.Select(n => n.SequenceId).ToList().ShouldBeEquivalentTo(new List<int> { 1 });

        route.ShiftSequenceIds(offset);
        route.Nodes.Select(n => n.SequenceId).ToList().ShouldBeEquivalentTo(new List<int> { 0 + offset, 2 + offset });
        route.Edges.Select(n => n.SequenceId).ToList().ShouldBeEquivalentTo(new List<int> { 1 + offset });
    }

    [TestMethod]
    public void Subsample_MaximumNodeNumberWithinRoute_ReturnsSelectedRoutePart()
    {
        var route = RouteGenerator.GenerateFromNodeIds(["1", "2", "3", "4"]);

        var routePart = route.Subsample(2, 2);

        routePart.Nodes.Select(n => n.NodeId).ToList().ShouldBeEquivalentTo(new List<string> { "2", "3" });
        routePart.Edges.Select(n => n.StartNodeId).ToList().ShouldBeEquivalentTo(new List<string> { "2" });
        routePart.Edges.Select(n => n.EndNodeId).ToList().ShouldBeEquivalentTo(new List<string> { "3" });
    }

    [TestMethod]
    public void Subsample_MaximumNodeNumberExceedsRoute_ReturnsSelectedRouteUntilRouteEnd()
    {
        var route = RouteGenerator.GenerateFromNodeIds(["1", "2", "3", "4"]);

        var routePart = route.Subsample(2, 42);

        routePart.Nodes.Select(n => n.NodeId).ToList().ShouldBeEquivalentTo(new List<string> { "2", "3", "4" });
        routePart.Edges.Select(n => n.StartNodeId).ToList().ShouldBeEquivalentTo(new List<string> { "2", "3" });
        routePart.Edges.Select(n => n.EndNodeId).ToList().ShouldBeEquivalentTo(new List<string> { "3", "4" });
    }

    [TestMethod]
    public void RemoveBeforeNode_ValidData_RouteUpdated()
    {
        var route = RouteGenerator.GenerateFromNodeIds(["1", "2", "3"]);

        var newStartNode = route.Nodes.Single(n => n.NodeId == "2");

        var shortenedRoute = route.RemoveBeforeNode(newStartNode);

        shortenedRoute.Nodes.Count.ShouldBe(2);
        shortenedRoute.Edges.Count.ShouldBe(1);

        shortenedRoute.Nodes.Select(n => n.NodeId).ToList().ShouldBeEquivalentTo(new List<string> { "2", "3" });
        shortenedRoute.Nodes.Select(n => n.SequenceId).ToList().ShouldBeEquivalentTo(new List<int> { 0, 2 });
        shortenedRoute.Edges.Select(n => n.SequenceId).ToList().ShouldBeEquivalentTo(new List<int> { 1 });
        shortenedRoute.Edges.Select(n => n.StartNodeId).ToList().ShouldBeEquivalentTo(new List<string> { "2" });
        shortenedRoute.Edges.Select(n => n.EndNodeId).ToList().ShouldBeEquivalentTo(new List<string> { "3" });
    }

    [TestMethod]
    public void Replace_ValidData_RouteReplacedStartingFromFirstNodeOfNewRoute()
    {
        var route = RouteGenerator.GenerateFromNodeIds(["1", "2", "3"]);
        var routeReplacement = RouteGenerator.GenerateFromNodeIds(["2", "8", "9"]);
        routeReplacement.ShiftSequenceIds(2);

        var newRoute = route.Replace(routeReplacement);

        newRoute.Nodes.Count.ShouldBe(4);
        newRoute.Edges.Count.ShouldBe(3);

        newRoute.Nodes.Select(n => n.NodeId).ToList().ShouldBeEquivalentTo(new List<string> { "1", "2", "8", "9" });
        newRoute.Nodes.Select(n => n.SequenceId).ToList().ShouldBeEquivalentTo(new List<int> { 0, 2, 4, 6 });
        newRoute.Edges.Select(n => n.SequenceId).ToList().ShouldBeEquivalentTo(new List<int> { 1, 3, 5 });
        newRoute.Edges.Select(n => n.StartNodeId).ToList().ShouldBeEquivalentTo(new List<string> { "1", "2", "8" });
        newRoute.Edges.Select(n => n.EndNodeId).ToList().ShouldBeEquivalentTo(new List<string> { "2", "8", "9" });
    }
}