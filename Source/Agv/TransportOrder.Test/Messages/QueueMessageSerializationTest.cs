using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Queue = Gebhardt.Acc.TransportOrder.Domain.Models.Queue.Queue;
using QueueNode = Gebhardt.Acc.Common.Messages.Queue.QueueNode;

namespace Gebhardt.Acc.TransportOrder.Test.Messages;

[TestClass]
public class QueueMessageSerializationTest
{
    private static readonly Guid _nodeId = "node id".ToGuid();
    private static readonly BaseNode _node = _nodeId.ToBaseNode();

    [TestMethod]
    public void GotQueues_ValidData_SerializesAllProperties()
    {
        var queue = new Queue("a queue");
        queue.AddNode(_nodeId, 0);
        var message = new GotQueues([queue]);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void GotQueue_ValidData_SerializesAllProperties()
    {
        var queue = new Queue("a queue");
        queue.AddNode(_nodeId, 0);
        var message = new GotQueue(queue);
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void AddQueue_ValidData_SerializesAllProperties()
    {
        var queue = new Queue("a queue");
        queue.AddNode(_nodeId, 0);
        var message = new AddQueue(queue.Name, queue.Nodes.Select(n => new QueueNode(_node, n.Position, null, null)).ToList());
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }

    [TestMethod]
    public void TryAddQueue_ValidData_SerializesAllProperties()
    {
        var queue = new Queue("a queue");
        queue.AddNode(_nodeId, 0);
        var message = new TryAddQueue(queue.Name, queue.Nodes.Select(n => new QueueNode(_node, n.Position, null, null)).ToList());
        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}