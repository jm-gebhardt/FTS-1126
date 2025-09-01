using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class QueueEventHandlerTest
{
    [TestMethod]
    public async Task Consume_QueueBecameEmpty_ValidData_QueueIsNotOccupied()
    {
        var fixture = new HostOrderConsumerTestFixture<QueueEventHandler>();

        await fixture.TestHarness.Bus.Publish(new QueueBecameEmpty());
        await fixture.ShouldHaveConsumedAsync<QueueBecameEmpty>();

        fixture.Configuration.QueueIsOccupied.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_QueueEntered_ValidData_QueueIsOccupied()
    {
        var fixture = new HostOrderConsumerTestFixture<QueueEventHandler>();

        await fixture.TestHarness.Bus.Publish(new QueueEntered());
        await fixture.ShouldHaveConsumedAsync<QueueEntered>();

        fixture.Configuration.QueueIsOccupied.ShouldBeTrue();
    }
}