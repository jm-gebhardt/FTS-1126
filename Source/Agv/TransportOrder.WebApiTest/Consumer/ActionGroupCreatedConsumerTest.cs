using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class ActionGroupCreatedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupCreated_SendAgvStateToAllClients()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupCreatedConsumer, IActionHubContext>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness!.Bus.Publish(new ActionGroupCreated(actionGroup, actionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionGroupCreated>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<ActionGroupCreated>()).ShouldBeTrue();

        fixture.MockHub.Verify(h => h.SendActionGroupStateToAllClients(
            It.Is<ActionGroup>(o => o.CorrelationId == actionGroup.CorrelationId)), Times.Once);
    }
}