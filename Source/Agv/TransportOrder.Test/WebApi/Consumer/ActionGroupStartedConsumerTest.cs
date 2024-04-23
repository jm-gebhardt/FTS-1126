using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionGroupStartedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupStarted_SendsAssignedActionGroupChanged()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupStartedConsumer, IAgvPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));

        (await fixture.TestHarness.Published.Any<ActionGroupStarted>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupStarted>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendAssignedActionGroupChangedAsync(
            actionGroup.AssignedAgv!.Value, It.Is<Guid>(a => a == actionGroup.CorrelationId)), Times.Once);
    }
}