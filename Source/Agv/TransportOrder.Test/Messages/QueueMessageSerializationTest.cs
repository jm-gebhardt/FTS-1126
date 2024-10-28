using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QueueNode = Gebhardt.Acc.Common.Messages.Queue.QueueNode;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class QueueMessageSerializationTest
{
    [TestMethod]
    public void GotQueues_ValidData_SerializesAllProperties()
    {
        var queue = new Queue("a queue");
        queue.AddNode("node id", 0);
        var message = new GotQueues([queue]);
        var fixture =  new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void GotQueue_ValidData_SerializesAllProperties()
    {
        var queue = new Queue("a queue");
        queue.AddNode("node id", 0);
        var message = new GotQueue(queue);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void AddQueue_ValidData_SerializesAllProperties()
    {
        var queue = new Queue("a queue");
        queue.AddNode("node id", 0);
        var message = new AddQueue(queue.Name, queue.Nodes.Select(n => new QueueNode(n.NodeId, n.Position)).ToList());
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void TryAddQueue_ValidData_SerializesAllProperties()
    {
        var queue = new Queue("a queue");
        queue.AddNode("node id", 0);
        var message = new TryAddQueue(queue.Name, queue.Nodes.Select(n => new QueueNode(n.NodeId, n.Position)).ToList());
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}