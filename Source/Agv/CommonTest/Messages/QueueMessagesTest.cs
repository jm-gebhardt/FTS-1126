using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Testing.Fixtures;

namespace Gebhardt.Acc.CommonTest.Messages;

[TestClass]
public class QueueMessagesTest
{
    [TestMethod]
    public void AddQueue_ValidData_MessageSerialized()
    {
        var fixture = new MessageSerialisationTestFixture();
        var addQueue = new AddQueue("queueName", [new QueueNode("nodeId", 0)]);

        fixture.ValidateSerialization(addQueue);
    }
}