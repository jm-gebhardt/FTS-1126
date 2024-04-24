using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest;

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
        var node1 = NodeGenerator.GenerateNode(AgvGraph.NodeType.Charge, 1f, 2f, 3f, nodeId:"1");
        var node2 = NodeGenerator.GenerateNode(AgvGraph.NodeType.Charge, 1f, 2f, 3f, nodeId:"2");
        var node3 = NodeGenerator.GenerateNode(AgvGraph.NodeType.Charge, 1f, 2f, 3f, nodeId:"1");
        var node4 = NodeGenerator.GenerateNode(AgvGraph.NodeType.Park, 0f, 0f, nodeId:"1");

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