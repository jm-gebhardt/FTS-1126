using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Node;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Node;

[TestClass]
public class NodeCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_FreeNode_ValidData_FreeNodeCalled()
    {
        var fixture = new NodeCommandHandlerTestFixture();

        await fixture.TestHarness!.Bus.Publish(new FreeNode("Charge-0"));

        (await fixture.ConsumerHarness!.Consumed.Any<FreeNode>()).ShouldBeTrue();
        fixture.NodeStateRegistry.Verify(x => x.FreeNodeAsync(It.IsAny<string>()), Times.Once);
    }
}