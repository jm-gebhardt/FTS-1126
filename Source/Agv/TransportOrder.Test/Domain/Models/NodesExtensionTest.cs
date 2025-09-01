using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class NodesExtensionTest
{
    [TestMethod]
    public void SelectById_ValidData_ReturnsNode()
    {
        var node = NodeGenerator.GenerateNode(nodeId:"id");
        var nodes = new List<GraphNode> { node };
        nodes.SelectById(node.Id).ShouldBe(node);
    }

    [TestMethod]
    public void SelectById_NodeNotInCollection_ThrowsNodeNotFoundException()
    {
        Should.Throw<NodeNotFoundException>(() => new List<GraphNode>().SelectById("a node that is not in the collection"));
    }
}