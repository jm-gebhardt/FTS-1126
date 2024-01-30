using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Models.Graphs;

[TestClass]
public class GraphTest
{
    [TestMethod]
    public void Merge_WithDeviatingEdgeFromNodeIds_ThrowsException()
    {
        var id = Guid.NewGuid();
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate(fromNodeId: "fr", id: id))
            .WithAgvGraphEdges(EdgeGenerator.Generate(fromNodeId: "fr2", id: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingEdgeToNodeIds_ThrowsException()
    {
        var id = Guid.NewGuid();
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate(toNodeId: "to", id: id))
            .WithAgvGraphEdges(EdgeGenerator.Generate(toNodeId: "to2", id: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingEdgeLengths_ThrowsException()
    {
        var id = Guid.NewGuid();
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate(length: 2, id: id))
            .WithAgvGraphEdges(EdgeGenerator.Generate(length: 2.5, id: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingEdgeVariants_ThrowsException()
    {
        var id = Guid.NewGuid();
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate(variant: EdgeType.Bezier1, id: id))
            .WithAgvGraphEdges(EdgeGenerator.Generate(variant: EdgeType.Bezier2, id: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingEdgeVariantParameters_ThrowsException()
    {
        var id = Guid.NewGuid();
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate(variantParameters: new List<EdgeCoefficient> { new(1) }, id: id))
            .WithAgvGraphEdges(EdgeGenerator.Generate(variantParameters: new List<EdgeCoefficient> { new(2) }, id: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingEdgeCountOfVariantParameters_ThrowsException()
    {
        var id = Guid.NewGuid();
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate(variantParameters: new List<EdgeCoefficient>(), id: id))
            .WithAgvGraphEdges(EdgeGenerator.Generate(variantParameters: new List<EdgeCoefficient> { new(2) }, id: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDuplicateEdges_ThrowsException()
    {
        var graphEdge = EdgeGenerator.Generate();
        var fixture = new GraphTestFixture()
            .WithAgvGraphEdges(graphEdge, graphEdge)
            .Build();

        Should.Throw<ArgumentException>(() => fixture.Graph.Merge(fixture.AgvGraph));
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
    public void Merge_WithDeviatingNodeType_ThrowsException()
    {
        const string id = "id";
        var fixture = new GraphTestFixture()
            .WithGraphNodes(NodeGenerator.GenerateNode(nodeType: AgvGraph.NodeType.Charge, nodeId: id))
            .WithAgvGraphNodes(NodeGenerator.GenerateNode(nodeType: AgvGraph.NodeType.Pick, nodeId: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingNodeX_ThrowsException()
    {
        const string id = "id";
        var fixture = new GraphTestFixture()
            .WithGraphNodes(NodeGenerator.GenerateNode(x: 2, nodeId: id))
            .WithAgvGraphNodes(NodeGenerator.GenerateNode(x: 2.5f, nodeId: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingNodeY_ThrowsException()
    {
        const string id = "id";
        var fixture = new GraphTestFixture()
            .WithGraphNodes(NodeGenerator.GenerateNode(y: 3, nodeId: id))
            .WithAgvGraphNodes(NodeGenerator.GenerateNode(y: 3.5f, nodeId: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingNodeZ_ThrowsException()
    {
        const string id = "id";
        var fixture = new GraphTestFixture()
            .WithGraphNodes(NodeGenerator.GenerateNode(z: 4, nodeId: id))
            .WithAgvGraphNodes(NodeGenerator.GenerateNode(z: 4.5f, nodeId: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
    }

    [TestMethod]
    public void Merge_WithDeviatingNodeTheta_ThrowsException()
    {
        const string id = "id";
        var fixture = new GraphTestFixture()
            .WithGraphNodes(NodeGenerator.GenerateNode(theta: 5, nodeId: id))
            .WithAgvGraphNodes(NodeGenerator.GenerateNode(theta: 5.5f, nodeId: id))
            .Build();

        Should.Throw<InvalidOperationException>(() => fixture.Graph.Merge(fixture.AgvGraph));
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
    public void Merge_WithEdgesAndNodes_MergesGraph()
    {
        var fixture = new GraphTestFixture()
            .WithGraphEdges(EdgeGenerator.Generate())
            .WithGraphNodes(NodeGenerator.GenerateNode())
            .WithAgvGraphEdges(EdgeGenerator.Generate(id: Guid.NewGuid()))
            .WithAgvGraphNodes(NodeGenerator.GenerateNode(nodeId: "new Id"))
            .Build();

        fixture.Graph.Merge(fixture.AgvGraph).ShouldBeTrue();
        fixture.Graph.Edges.Count.ShouldBe(2);
        fixture.Graph.Nodes.Count.ShouldBe(2);
    }
}