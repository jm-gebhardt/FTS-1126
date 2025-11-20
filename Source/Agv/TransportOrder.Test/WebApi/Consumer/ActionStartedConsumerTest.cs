using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionStartedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionStartedByAgv_SendsAssignedActionGroupChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, ["1".ToGraphNode(), "2".ToGraphNode()], []);
        var fixture = new ConsumerHubTestFixture<ActionStartedConsumer, IAgvPublisher>();
        Mock<IAgvRouteRetrievalService> agvRouteRetrievalService = fixture.AgvRouteServiceMock;
        agvRouteRetrievalService.Setup(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<Guid?>())).ReturnsAsync(agvRoute);
        var action = AgvActionGenerator.GenerateMoveAction();

        await fixture.TestHarness.Bus.Publish(new ActionStartedByAgv(action.CorrelationId, agvCorrelationId));

        await fixture.ShouldHavePublishedAsync<ActionStartedByAgv>();
        await fixture.ShouldHaveConsumedAsync<ActionStartedByAgv>();
        agvRouteRetrievalService.Verify(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<Guid?>()), Times.Once);
        fixture.PublisherMock.Verify(h => h.SendAgvRouteChangedAsync(agvRoute), Times.Once);
    }

    [TestMethod]
    public async Task Receiving_ActionUpdatedByAgv_SendsAssignedActionGroupChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, ["1".ToGraphNode(), "2".ToGraphNode()], []);
        var fixture = new ConsumerHubTestFixture<ActionStartedConsumer, IAgvPublisher>();
        Mock<IAgvRouteRetrievalService> agvRouteRetrievalService = fixture.AgvRouteServiceMock;
        agvRouteRetrievalService.Setup(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<Guid?>())).ReturnsAsync(agvRoute);
        var action = AgvActionGenerator.GenerateMoveAction();

        await fixture.TestHarness.Bus.Publish(new ActionUpdatedByAgv(action.CorrelationId, agvCorrelationId));

        await fixture.ShouldHavePublishedAsync<ActionUpdatedByAgv>();
        await fixture.ShouldHaveConsumedAsync<ActionUpdatedByAgv>();
        agvRouteRetrievalService.Verify(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<Guid?>()), Times.Once);
        fixture.PublisherMock.Verify(h => h.SendAgvRouteChangedAsync(agvRoute), Times.Once);
    }
}