using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionGroupConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionGroupStarted_SendsAssignedActionGroupChanged()
    {
        var fixture = new ConsumerHubTestFixture<AgvActionGroupConsumer, IAgvPublisher>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));
        
        await fixture.ShouldHavePublishedAsync<ActionGroupStarted>();
        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();

        fixture.PublisherMock.Verify(h => h.SendAssignedActionGroupChangedAsync(
            actionGroup.AssignedAgv!.Value, It.Is<Guid>(a => a == actionGroup.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_ValidData_SentAgvRouteChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId);
        var fixture = new ConsumerHubTestFixture<AgvActionGroupConsumer, IAgvPublisher>();

        await fixture.PublishMessageAsync(new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow));
        fixture.PublisherMock.Verify(h => h.SendAgvRouteChangedAsync(new CurrentAgvRouteReadModel(agvCorrelationId, Array.Empty<string>(), Array.Empty<Guid>())),
            Times.Once);
    }
}