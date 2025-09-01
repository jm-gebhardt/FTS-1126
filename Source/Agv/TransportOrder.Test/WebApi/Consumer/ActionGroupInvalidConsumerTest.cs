using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionGroupInvalidConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupInvalid_SendsAgvState()
    {
        var fixture = new ConsumerHubTestFixture<ActionGroupInvalidConsumer, IActionGroupPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        await fixture.TestHarness.Bus.Publish(new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, new List<string> { "Error" }));

        await fixture.ShouldHavePublishedAsync<ActionGroupInvalid>();
        await fixture.ShouldHaveConsumedAsync<ActionGroupInvalid>();

        fixture.PublisherMock.Verify(h => h.SendActionGroupChangedAsync(
            It.IsAny<ActionGroupReadModel>()), Times.Once);
    }
}