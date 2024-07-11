using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class TriggerEventHandlerTest
{
    [TestMethod]
    public async Task Consume_TriggerConfigured_ValidData_PublishesCreateTrigger()
    {
        var fixture = new TriggerEventHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new TriggerConfigured(new TriggerProperties("urn", "name", "description", "version"), []));

        await fixture.ShouldHaveConsumedAsync<TriggerConfigured>();
        await fixture.ShouldHavePublishedAsync<CreateTrigger>();
    }
}