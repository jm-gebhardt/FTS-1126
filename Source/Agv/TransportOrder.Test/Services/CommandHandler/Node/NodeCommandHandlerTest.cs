using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Node;

[TestClass]
public class NodeCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_FreeNode_ValidData_FreeNodeCalled()
    {
        const string nodeId = "Charge-0";
        var fixture = new NodeCommandHandlerTestFixture().WithNodeState(new NodeLock(nodeId, NodeLockState.Free, false, null, DateTime.UtcNow));

        await fixture.TestHarness.Bus.Publish(new FreeNode(nodeId));

        (await fixture.ConsumerHarness.Consumed.Any<FreeNode>()).ShouldBeTrue();
        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<string>()), Times.Once);
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
    }
}