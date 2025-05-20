using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class TriggerChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_TriggerChanged_ValidData_SendsTriggerChanged()
    {
        var fixture = new ConsumerHubTestFixture<TriggerChangedConsumer, ITriggerPublisher>();
        var trigger = new Trigger(Guid.NewGuid(), "urn", "a trigger", "version", "description", null, []);

        await fixture.TestHarness.Bus.Publish(new TriggerChanged(trigger));
        await fixture.ShouldHaveConsumedAsync<TriggerChanged>();

        fixture.PublisherMock.Verify(h => h.SendTriggerChangedAsync(It.IsAny<Trigger>()), Times.Once);
    }
}