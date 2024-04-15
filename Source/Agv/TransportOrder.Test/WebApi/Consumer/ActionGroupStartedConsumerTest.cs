using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Consumer;

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