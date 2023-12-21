using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest;

/// <summary>
/// Test for utility <see cref="StringExtensions"/>.
/// </summary>
[TestClass]
public class UtilitiesTest
{
    private const string TestPattern = "#HashMe#";
    private const string HashPattern = "a53178cb-6f56-b8b8-638e-3a2447004b78";

    [TestMethod]
    public void String_ToGuid()
    {
        var expected = new Guid(HashPattern);
        var current = TestPattern.ToGuid();
        current.ShouldBe(expected);
        current = TestPattern.ToGuid();
        current.ShouldBe(expected);
    }

    /// <summary>
    /// Test node comparer by ID
    /// </summary>
    [TestMethod]
    public void NodeIdComparer()
    {
        var comparer = new GraphNodeComparer();
        var node1 = new GraphNode("1") { X = 1f, Y = 2f, Z = 3f, Type = AgvGraph.NodeType.Charge };
        var node2 = new GraphNode("2") { X = 1f, Y = 2f, Z = 3f, Type = AgvGraph.NodeType.Charge };
        var node3 = new GraphNode("1") { X = 1f, Y = 2f, Z = 3f, Type = AgvGraph.NodeType.Charge };
        var node4 = new GraphNode("1") { X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.Park };

        comparer.Equals(node1, node2).ShouldBeFalse();
        comparer.GetHashCode(node2).ShouldNotBe(comparer.GetHashCode(node1));

        comparer.Equals(node1, node3).ShouldBeTrue();
        comparer.GetHashCode(node1).ShouldBe(comparer.GetHashCode(node3));

        comparer.Equals(node1, node4).ShouldBeTrue();
        comparer.GetHashCode(node1).ShouldBe(comparer.GetHashCode(node4));
    }

    /// <summary>
    /// Test edge comparer by from/to
    /// </summary>
    [TestMethod]
    public void EdgeIdComparer()
    {
        var comparer = new GraphEdgeComparer();
        var edge1 = GraphEdge.CreateSlanted("1", "2", 1f, 90, 15d);
        var edge2 = GraphEdge.CreateDirect("2", "1");
        var edge3 = GraphEdge.CreateSlanted("1", "2", 2f, 90d, 15d);

        comparer.Equals(edge1, edge2).ShouldBeFalse();
        comparer.GetHashCode(edge1).ShouldNotBe(comparer.GetHashCode(edge2));

        comparer.Equals(edge1, edge3).ShouldBeTrue();
        comparer.GetHashCode(edge1).ShouldBe(comparer.GetHashCode(edge3));
    }
}