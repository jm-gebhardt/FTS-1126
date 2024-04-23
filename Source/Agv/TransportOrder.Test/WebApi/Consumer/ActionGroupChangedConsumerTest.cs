using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionGroupChangedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupChanged_SendsActionGroupChanged()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupChangedConsumer, IActionGroupPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness.Bus.Publish(new ActionGroupChanged(actionGroup, actionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendActionGroupChangedAsync(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }
}