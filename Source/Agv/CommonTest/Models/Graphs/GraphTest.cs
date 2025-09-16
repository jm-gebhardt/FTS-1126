using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Models.Graphs;

[TestClass]
public class GraphTest
{
    [TestMethod]
    public void Merge_WithUpdatedNode_Updates()
    {
        const string id = "1";
        var timeStamp = DateTime.UtcNow;
        var graphNode = NodeGenerator.GenerateNode(nodeName: id, timeStamp: timeStamp);
        var agvGraphNode = NodeGenerator.GenerateNode(nodeName: id, x: 123, timeStamp: timeStamp.AddHours(1));
        var fixture = new GraphTestFixture()
            .WithGraphNodes(graphNode)
            .WithAgvGraphNodes(agvGraphNode)
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeTrue();
        fixture.Graph.Nodes.SingleOrDefault(n => n.Name == id).ShouldBe(agvGraphNode);
    }

    [TestMethod]
    public void Merge_WithUpdatedEdge_Updates()
    {
        var id = Guid.NewGuid();
        var timeStamp = DateTime.UtcNow;
        var graphEdge = EdgeGenerator.Generate(id: id, variant: EdgeType.Direct, timeStamp: timeStamp);
        var agvGraphEdge = EdgeGenerator.Generate(id: id, variant: EdgeType.Polypath, timeStamp: timeStamp.AddHours(1));
        var fixture = new GraphTestFixture()
            .WithGraphEdges(graphEdge)
            .WithAgvGraphEdges(agvGraphEdge)
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeTrue();
        fixture.Graph.Edges.SingleOrDefault(e => e.Id == id).ShouldBe(agvGraphEdge);
    }

    [TestMethod]
    public void Merge_WithSameEdges_MergesGraph()
    {
        var graphEdge = EdgeGenerator.Generate(fromNodeId: "fr", id: Guid.NewGuid());
        var fixture = new GraphTestFixture()
            .WithGraphEdges(graphEdge)
            .WithAgvGraphEdges(graphEdge)
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeFalse();
        fixture.Graph.Edges.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Merge_WithEqualEdges_MergesGraph()
    {
        var id = Guid.NewGuid();
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate(id: id))
            .WithAgvGraphEdges(EdgeGenerator.Generate(id: id))
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeFalse();
        fixture.Graph.Edges.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Merge_WithSameNodes_MergesGraph()
    {
        var graphNode = NodeGenerator.GenerateNode();
        var fixture = new GraphTestFixture()
            .WithGraphNodes(graphNode)
            .WithAgvGraphNodes(graphNode)
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeFalse();
        fixture.Graph.Nodes.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Merge_WithEqualNodes_MergesGraph()
    {
        var fixture = new GraphTestFixture()
            .WithGraphNodes(NodeGenerator.GenerateNode())
            .WithAgvGraphNodes(NodeGenerator.GenerateNode())
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeFalse();
        fixture.Graph.Nodes.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Merge_WithEqualNodesWithMultipleTypes_MergesGraph()
    {
        var fixture = new GraphTestFixture()
            .WithGraphNodes(NodeGenerator.GenerateNodeWithMultipleTypes([AgvGraph.NodeType.PickDrop]))
            .WithAgvGraphNodes(NodeGenerator.GenerateNodeWithMultipleTypes([AgvGraph.NodeType.PickDrop]))
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeFalse();
        fixture.Graph.Nodes.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Merge_WithEdgesAndNodes_MergesGraph()
    {
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate())
            .WithGraphNodes(NodeGenerator.GenerateNode())
            .WithAgvGraphEdges(EdgeGenerator.Generate(id: Guid.NewGuid()))
            .WithAgvGraphNodes(NodeGenerator.GenerateNode(nodeName: "new Id"))
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeTrue();
        fixture.Graph.Edges.Count.ShouldBe(2);
        fixture.Graph.Nodes.Count.ShouldBe(2);
    }
    
    [TestMethod]
    public void Merge_WithRemovedEdge_DoNotRemoveEdge()
    {
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate())
            .WithAgvGraphEdges()
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeFalse();
        fixture.Graph.Edges.Count.ShouldBe(1);
    }
    
    [TestMethod]
    public void Merge_WithRemovedNode_DoNotRemoveNode()
    {
        var fixture = new GraphTestFixture()
            .WithGraphNodes(NodeGenerator.GenerateNode())
            .WithAgvGraphNodes()
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeFalse();
        fixture.Graph.Nodes.Count.ShouldBe(1);
    }
}