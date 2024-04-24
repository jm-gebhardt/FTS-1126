using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
        
        await fixture.ShouldHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();

        fixture.PublisherMock.Verify(h => h.SendAssignedActionGroupChangedAsync(
            actionGroup.AssignedAgv!.Value, It.Is<Guid>(a => a == actionGroup.CorrelationId)), Times.Once);
    }
}