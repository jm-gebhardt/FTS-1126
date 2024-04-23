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
public class ActionGroupCreatedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupCreated_SendsActionGroupCreated()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupCreatedConsumer, IActionGroupPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness.Bus.Publish(new ActionGroupCreated(actionGroup, actionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendActionGroupCreatedAsync(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }
}