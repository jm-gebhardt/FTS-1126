using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Extensions;

[TestClass]
public class NodeExtensionTest
{
    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.PickDrop)]
    public void IsPickStation_PickTypes_True(AgvGraph.NodeType nodeType)
    {
        var node = new GraphNode("id") { Type = nodeType };

        node.IsPickStation().ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Drop)]
    [DataRow(AgvGraph.NodeType.Charge)]
    [DataRow(AgvGraph.NodeType.WayPoint)]
    public void IsPickStation_NonPickTypes_False(AgvGraph.NodeType nodeType)
    {
        var node = new GraphNode("id") { Type = nodeType };

        node.IsPickStation().ShouldBeFalse();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Drop)]
    [DataRow(AgvGraph.NodeType.PickDrop)]
    public void IsDropStation_DropTypes_True(AgvGraph.NodeType nodeType)
    {
        var node = new GraphNode("id") { Type = nodeType };

        node.IsDropStation().ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.Charge)]
    [DataRow(AgvGraph.NodeType.WayPoint)]
    public void IsDropStation_NonDropTypes_False(AgvGraph.NodeType nodeType)
    {
        var node = new GraphNode("id") { Type = nodeType };

        node.IsDropStation().ShouldBeFalse();
    }
}