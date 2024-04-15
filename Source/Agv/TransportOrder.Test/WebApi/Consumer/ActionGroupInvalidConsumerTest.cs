using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionGroupInvalidConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupInvalid_SendsAgvState()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupInvalidConsumer, IActionGroupPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness.Bus.Publish(new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, new List<string> { "Error" }));

        (await fixture.TestHarness.Published.Any<ActionGroupInvalid>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupInvalid>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendActionGroupChangedAsync(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }
}