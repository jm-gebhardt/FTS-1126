using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain;

[TestClass]
public class RouteTest
{
    // VDA5050 sequence layout: node(0), edge(1), node(2), edge(3), node(4), ...

    private static OrderNode Node(string id, int sequenceId, bool released = true) =>
        new() { NodeId = id, SequenceId = sequenceId, Released = released };

    private static OrderEdge Edge(string id, int sequenceId, string startNodeId, string endNodeId, bool released = true) =>
        new() { EdgeId = id, SequenceId = sequenceId, StartNodeId = startNodeId, EndNodeId = endNodeId, Released = released };

    [TestMethod]
    public void Proceed_WithZeroSequenceId_PreservesAllNodesAndEdges()
    {
        var route = new Route(
            [Node("A", 0), Node("B", 2)],
            [Edge("e1", 1, "A", "B")]);

        route.Proceed(0);

        route.Nodes.Count.ShouldBe(2);
        route.Edges.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Proceed_WithPassedNodeSequenceId_TrimsPrecedingNodes()
    {
        // Passing node B (seqId 2) should drop node A (seqId 0)
        var route = new Route(
            [Node("A", 0), Node("B", 2), Node("C", 4)],
            [Edge("e1", 1, "A", "B"), Edge("e2", 3, "B", "C")]);

        route.Proceed(2);

        route.Nodes.Select(n => n.NodeId).ShouldBe(["B", "C"], ignoreOrder: true);
    }

    [TestMethod]
    public void Proceed_WithPassedNodeSequenceId_KeepsCurrentEdge()
    {
        // Passing node B (seqId 2) keeps edge A→B (seqId 1) because seqId >= passedNodeSequenceId - 1
        var route = new Route(
            [Node("A", 0), Node("B", 2), Node("C", 4)],
            [Edge("e1", 1, "A", "B"), Edge("e2", 3, "B", "C")]);

        route.Proceed(2);

        route.Edges.Select(e => e.EdgeId).ShouldBe(["e1", "e2"], ignoreOrder: true);
    }

    [TestMethod]
    public void Proceed_AtLastNodeSequenceId_LeavesOnlyLastNode()
    {
        var route = new Route(
            [Node("A", 0), Node("B", 2), Node("C", 4)],
            [Edge("e1", 1, "A", "B"), Edge("e2", 3, "B", "C")]);

        route.Proceed(4);

        route.Nodes.Single().NodeId.ShouldBe("C");
        route.Edges.Select(e => e.EdgeId).ShouldBe(["e2"], ignoreOrder: true);
    }

    [TestMethod]
    public void Proceed_WithSequenceIdBeyondAllNodes_ClearsNodes()
    {
        var route = new Route(
            [Node("A", 0), Node("B", 2)],
            [Edge("e1", 1, "A", "B")]);

        route.Proceed(10);

        route.Nodes.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetBase_WithMixedReleasedNodes_ReturnsOnlyReleasedNodes()
    {
        var route = new Route(
            [Node("A", 0, released: true), Node("B", 2, released: false), Node("C", 4, released: true)],
            []);

        var routeBase = route.GetBase();

        routeBase.Nodes.Select(n => n.NodeId).ShouldBe(["A", "C"], ignoreOrder: true);
    }

    [TestMethod]
    public void GetBase_WithMixedReleasedEdges_ReturnsOnlyReleasedEdges()
    {
        var route = new Route(
            [Node("A", 0), Node("B", 2), Node("C", 4)],
            [Edge("e1", 1, "A", "B", released: true), Edge("e2", 3, "B", "C", released: false)]);

        var routeBase = route.GetBase();

        routeBase.Edges.Single().EdgeId.ShouldBe("e1");
    }

    [TestMethod]
    public void GetBase_OrdersNodesBySequenceId()
    {
        // Provide nodes out of order to verify sorting
        var route = new Route(
            [Node("C", 4), Node("A", 0), Node("B", 2)],
            []);

        var routeBase = route.GetBase();

        routeBase.Nodes.Select(n => n.NodeId).ShouldBe(["A", "B", "C"]);
    }

    [TestMethod]
    public void GetBase_OrdersEdgesBySequenceId()
    {
        var route = new Route(
            [Node("A", 0), Node("B", 2), Node("C", 4)],
            [Edge("e2", 3, "B", "C"), Edge("e1", 1, "A", "B")]);

        var routeBase = route.GetBase();

        routeBase.Edges.Select(e => e.EdgeId).ShouldBe(["e1", "e2"]);
    }

    [TestMethod]
    public void GetBase_WithNoReleasedElements_ReturnsEmptyCollections()
    {
        var route = new Route(
            [Node("A", 0, released: false)],
            [Edge("e1", 1, "A", "B", released: false)]);

        var routeBase = route.GetBase();

        routeBase.Nodes.ShouldBeEmpty();
        routeBase.Edges.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetBase_DoesNotModifyOriginalRoute()
    {
        var route = new Route(
            [Node("A", 0, released: true), Node("B", 2, released: false)],
            []);

        route.GetBase();

        route.Nodes.Count.ShouldBe(2);
    }
}
