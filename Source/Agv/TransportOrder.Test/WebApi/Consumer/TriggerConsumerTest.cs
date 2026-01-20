using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class TriggerConsumerTest
{
    [TestMethod]
    public async Task Consume_TriggerChanged_ValidData_SendsTriggerChanged()
    {
        var fixture = new ConsumerHubTestFixture<TriggerConsumer, ITriggerPublisher>();
        var trigger = TriggerGenerator.CreateTrigger();

        await fixture.TestHarness.Bus.Publish(new TriggerChanged(trigger));
        await fixture.ShouldHaveConsumedAsync<TriggerChanged>();

        fixture.PublisherMock.Verify(h => h.SendTriggerChangedAsync(It.IsAny<Trigger>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_TriggerMarkedForDeletion_ValidData_SendsTriggerDeleted()
    {
        var fixture = new ConsumerHubTestFixture<TriggerConsumer, ITriggerPublisher>();

        await fixture.PublishMessageAsync(new TriggerMarkedForDeletion(Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<TriggerMarkedForDeletion>();

        fixture.PublisherMock.Verify(h => h.SendTriggerDeletedAsync(It.IsAny<Guid>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_TriggerRestored_ValidData_SendsTriggerRestored()
    {
        var fixture = new ConsumerHubTestFixture<TriggerConsumer, ITriggerPublisher>();
        var trigger = TriggerGenerator.CreateTrigger();

        await fixture.PublishMessageAsync(new TriggerRestored(trigger));
        await fixture.ShouldHaveConsumedAsync<TriggerRestored>();

        fixture.PublisherMock.Verify(h => h.SendTriggerChangedAsync(It.IsAny<Trigger>()), Times.Once);
    }
}