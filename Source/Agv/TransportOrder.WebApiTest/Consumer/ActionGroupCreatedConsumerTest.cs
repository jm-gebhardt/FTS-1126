using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class ActionGroupCreatedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupCreated_SendsActionGroupCreated()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupCreatedConsumer, IActionGroupPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness!.Bus.Publish(new ActionGroupCreated(actionGroup, actionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.MockHub.Verify(h => h.SendActionGroupCreatedAsync(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }
}