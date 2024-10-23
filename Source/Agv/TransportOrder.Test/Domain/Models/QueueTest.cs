using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class QueueTest
{
    [TestMethod]
    public void AddNode_ValidData_NodeAdded()
    {
        var queue = new Queue("a queue");
        const string nodeId = "a node id";
        queue.AddNode(nodeId, 0);

        queue.Nodes.Count.ShouldBe(1);
        var node = queue.Nodes.First();
        node.NodeId.ShouldBe(nodeId);
        node.Position.ShouldBe(0);
    }

    [TestMethod]
    public void AddNode_NodeWithIdAlreadyExists_ThrowsQueueNodeExistsException()
    {
        var queue = new Queue("a queue");
        const string nodeId = "a node id";
        queue.AddNode(nodeId, 0);

        Should.Throw<QueueNodeExistsException>(() => queue.AddNode(nodeId, 1));
    }

    [TestMethod]
    public void AddNode_NodeAtPositionAlreadyExists_ThrowsQueueNodeExistsException()
    {
        var queue = new Queue("a queue");
        const int position = 0;
        queue.AddNode("node id", position);

        Should.Throw<QueueNodeExistsException>(() => queue.AddNode("another node id", position));
    }

    [TestMethod]
    public void RemoveNode_ValidData_RemovesNode()
    {
        var queue = new Queue("a queue");
        const string nodeId = "a node id";
        queue.AddNode(nodeId, 0);

        queue.RemoveNode(nodeId);

        queue.Nodes.ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveNode_NodeWithIdDoesNotExist_ThrowsNodeNotFoundException()
    {
        var queue = new Queue("a queue");
        const string nodeId = "a node id";
        queue.AddNode(nodeId, 0);

        Should.Throw<NodeNotFoundException>(() => queue.RemoveNode("unknown"));
    }
}