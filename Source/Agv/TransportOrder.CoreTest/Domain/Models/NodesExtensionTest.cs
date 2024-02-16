using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Models;

[TestClass]
public class NodesExtensionTest
{
    [TestMethod]
    public void SelectById_ValidData_ReturnsNode()
    {
        var node = new GraphNode("id");
        var nodes = new List<GraphNode> { node };
        nodes.SelectById(node.Id).ShouldBe(node);
    }

    [TestMethod]
    public void SelectById_NodeNotInCollection_ThrowsNodeNotFoundException()
    {
        Should.Throw<NodeNotFoundException>(() => new List<GraphNode>().SelectById("a node that is not in the collection"));
    }
}