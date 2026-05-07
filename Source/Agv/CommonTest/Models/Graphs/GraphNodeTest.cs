using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;
using static Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.CommonTest.Models.Graphs;

[TestClass]
public class GraphNodeTest
{
    [TestMethod]
    public void Equals_SameReference_ReturnsTrue()
    {
        var node = NodeGenerator.GenerateNode();

        node.Equals(node).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_Null_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode();

        node.Equals(null).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_IdenticalNodes_ReturnsTrue()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 2f, z: 3f);
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 2f, z: 3f);

        node1.Equals(node2).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1");
        var node2 = NodeGenerator.GenerateNode(nodeName: "N2");

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentName_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var node1 = new GraphNode(id, "NodeA") { Type = [NodeType.WayPoint], X = 1f, Y = 2f, Z = 0f };
        var node2 = new GraphNode(id, "NodeB") { Type = [NodeType.WayPoint], X = 1f, Y = 2f, Z = 0f };

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentX_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 2f, z: 3f);
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", x: 9f, y: 2f, z: 3f);

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentY_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 2f, z: 3f);
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 9f, z: 3f);

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentZ_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 2f, z: 3f);
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 2f, z: 9f);

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentTheta_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", theta: 0f);
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", theta: 1.5f);

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentSingleNodeType_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", nodeType: NodeType.WayPoint);
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", nodeType: NodeType.Charge);

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_SingleTypeVsMatchingCollectionWithOneType_ReturnsTrue()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", nodeType: NodeType.WayPoint);
        var node2 = NodeGenerator.GenerateNodeWithTypeCollection(new[] { NodeType.WayPoint }, nodeName: "N1");

        node1.Equals(node2).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_SingleTypeVsCollectionWithMultipleTypes_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", nodeType: NodeType.WayPoint);
        var node2 = NodeGenerator.GenerateNodeWithTypeCollection(new[] { NodeType.WayPoint, NodeType.Charge }, nodeName: "N1");

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_CollectionWithOneTypeVsSingleMatchingType_ReturnsTrue()
    {
        var node1 = NodeGenerator.GenerateNodeWithTypeCollection(new[] { NodeType.WayPoint }, nodeName: "N1");
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", nodeType: NodeType.WayPoint);

        node1.Equals(node2).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_CollectionWithMultipleTypesVsSingleType_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNodeWithTypeCollection(new[] { NodeType.WayPoint, NodeType.Charge }, nodeName: "N1");
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", nodeType: NodeType.WayPoint);

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_IdenticalCollections_ReturnsTrue()
    {
        var node1 = NodeGenerator.GenerateNodeWithTypeCollection(new[] { NodeType.WayPoint, NodeType.Charge }, nodeName: "N1");
        var node2 = NodeGenerator.GenerateNodeWithTypeCollection(new[] { NodeType.WayPoint, NodeType.Charge }, nodeName: "N1");

        node1.Equals(node2).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_DifferentCollections_ReturnsFalse()
    {
        var node1 = NodeGenerator.GenerateNodeWithTypeCollection(new[] { NodeType.WayPoint, NodeType.Charge }, nodeName: "N1");
        var node2 = NodeGenerator.GenerateNodeWithTypeCollection(new[] { NodeType.WayPoint, NodeType.Park }, nodeName: "N1");

        node1.Equals(node2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_ObjectOverload_SameReference_ReturnsTrue()
    {
        var node = NodeGenerator.GenerateNode();
        object obj = node;

        node.Equals(obj).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_ObjectOverload_Null_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode();

        node.Equals((object?)null).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_ObjectOverload_IdenticalNode_ReturnsTrue()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1");
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1");
        object obj = node2;

        node1.Equals(obj).ShouldBeTrue();
    }

    [TestMethod]
    public void GetHashCode_IdenticalNodes_ReturnsSameHash()
    {
        var node1 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 2f, z: 3f);
        var node2 = NodeGenerator.GenerateNode(nodeName: "N1", x: 1f, y: 2f, z: 3f);

        node1.GetHashCode().ShouldBe(node2.GetHashCode());
    }
}
