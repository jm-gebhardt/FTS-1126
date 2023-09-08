using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class ActionGroupInvalidConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupInvalid_SendAgvStateToAllClients()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupInvalidConsumer, IActionHubContext>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness!.Bus.Publish(new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, new List<string>{"Error"}));

        (await fixture.TestHarness.Published.Any<ActionGroupInvalid>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<ActionGroupInvalid>()).ShouldBeTrue();

        fixture.MockHub.Verify(h => h.SendActionGroupStateToAllClients(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }
}
