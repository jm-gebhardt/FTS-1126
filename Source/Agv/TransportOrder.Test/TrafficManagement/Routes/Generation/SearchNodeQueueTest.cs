using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.TrafficManagement.NewGraph;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.TrafficManagement.Routes.Generation;

[TestClass]
public class SearchNodeQueueTest
{
    private static readonly AgvType _agvType = AgvTypeGenerator.GenerateAgvType();

    [TestMethod]
    public void TryEnqueue_EmptyQueue_ReturnsTrueAndNodeIsDequeued()
    {
        var queue = new SearchNodeQueue();
        var searchNode = CreateSearchNode(RoutingResourceGenerator.CreateRoutingNode(_agvType), 1);

        bool enqueued = queue.TryEnqueue(searchNode);

        enqueued.ShouldBeTrue();
        queue.TryDequeue(out var dequeuedSearchNode).ShouldBeTrue();
        dequeuedSearchNode.ShouldBeSameAs(searchNode);
    }

    [TestMethod]
    public void TryDequeue_EmptyQueue_ReturnsFalseAndNull()
    {
        var queue = new SearchNodeQueue();

        bool dequeued = queue.TryDequeue(out var searchNode);

        dequeued.ShouldBeFalse();
        searchNode.ShouldBeNull();
    }

    [TestMethod]
    public void TryDequeue_MultipleNodes_ReturnsNodesOrderedByTotalCost()
    {
        var queue = new SearchNodeQueue();
        var expensiveSearchNode = CreateSearchNode(RoutingResourceGenerator.CreateRoutingNode(_agvType), 5);
        var cheapSearchNode = CreateSearchNode(RoutingResourceGenerator.CreateRoutingNode(_agvType), 1);
        var mediumSearchNode = CreateSearchNode(RoutingResourceGenerator.CreateRoutingNode(_agvType), 3);

        queue.TryEnqueue(expensiveSearchNode);
        queue.TryEnqueue(cheapSearchNode);
        queue.TryEnqueue(mediumSearchNode);

        DequeueAll(queue).ShouldBe([cheapSearchNode, mediumSearchNode, expensiveSearchNode]);
    }

    [TestMethod]
    public void TryEnqueue_DominatedNodeForSameRoutingNode_ReturnsFalseAndKeepsExistingNode()
    {
        var queue = new SearchNodeQueue();
        var routingNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var dominatingSearchNode = CreateSearchNode(routingNode, 5);
        var dominatedSearchNode = CreateSearchNode(routingNode, 8);
        queue.TryEnqueue(dominatingSearchNode);

        bool enqueued = queue.TryEnqueue(dominatedSearchNode);

        enqueued.ShouldBeFalse();
        DequeueAll(queue).ShouldBe([dominatingSearchNode]);
    }

    [TestMethod]
    public void TryEnqueue_BetterNodeForSameRoutingNode_ReplacesExistingNode()
    {
        var queue = new SearchNodeQueue();
        var routingNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var dominatedSearchNode = CreateSearchNode(routingNode, 8);
        var dominatingSearchNode = CreateSearchNode(routingNode, 5);
        queue.TryEnqueue(dominatedSearchNode);

        bool enqueued = queue.TryEnqueue(dominatingSearchNode);

        enqueued.ShouldBeTrue();
        DequeueAll(queue).ShouldBe([dominatingSearchNode]);
    }

    [TestMethod]
    public void TryEnqueue_EqualTotalCostForSameRoutingNode_ReturnsFalseAndKeepsExistingNode()
    {
        var queue = new SearchNodeQueue();
        var routingNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var firstSearchNode = CreateSearchNode(routingNode, 5);
        var secondSearchNode = CreateSearchNode(routingNode, 5, firstSearchNode);
        queue.TryEnqueue(firstSearchNode);

        bool enqueued = queue.TryEnqueue(secondSearchNode);

        enqueued.ShouldBeFalse();
        DequeueAll(queue).ShouldBe([firstSearchNode]);
    }

    [TestMethod]
    public void TryEnqueue_AfterDequeueOfSameRoutingNode_KeepsDominationCheckAcrossProcessedNodes()
    {
        var queue = new SearchNodeQueue();
        var routingNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var processedSearchNode = CreateSearchNode(routingNode, 5);
        var dominatedSearchNode = CreateSearchNode(routingNode, 8);
        var dominatingSearchNode = CreateSearchNode(routingNode, 2);
        queue.TryEnqueue(processedSearchNode);
        queue.TryDequeue(out _);

        queue.TryEnqueue(dominatedSearchNode).ShouldBeFalse();
        queue.TryEnqueue(dominatingSearchNode).ShouldBeTrue();

        DequeueAll(queue).ShouldBe([dominatingSearchNode]);
    }

    private static SearchNode CreateSearchNode(RoutingNode routingNode, double totalCost, SearchNode? predecessor = null)
    {
        return new SearchNode(predecessor, null, routingNode, totalCost, totalCost);
    }

    private static List<SearchNode> DequeueAll(SearchNodeQueue queue)
    {
        var searchNodes = new List<SearchNode>();
        while (queue.TryDequeue(out var searchNode))
        {
            searchNodes.Add(searchNode);
        }
        return searchNodes;
    }
}
