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
public class ActionGroupChangedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupChanged_SendsActionGroupChanged()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupChangedConsumer, IActionGroupPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness!.Bus.Publish(new ActionGroupChanged(actionGroup, actionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<ActionGroupChanged>()).ShouldBeTrue();

        fixture.MockHub.Verify(h => h.SendActionGroupChangedAsync(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }
}
