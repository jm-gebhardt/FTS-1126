using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class ActionGroupChangedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupChanged_SendAgvStateToAllClients()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupChangedConsumer, IActionHubContext>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness!.Bus.Publish(new ActionGroupChanged(actionGroup, actionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionGroupChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<ActionGroupChanged>()).ShouldBeTrue();

        fixture.MockHub.Verify(h => h.SendActionGroupStateToAllClients(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }
}
