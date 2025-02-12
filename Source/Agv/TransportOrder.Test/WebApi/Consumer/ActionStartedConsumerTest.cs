using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionStartedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionStarted_SendsAssignedActionGroupChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, new[] { "1", "2" }, []);
        var fixture = new ConsumerHubTestFixture<ActionStartedConsumer, IAgvPublisher>();
        Mock<IAgvRouteRetrievalService> agvRouteRetrievalService = fixture.AgvRouteServiceMock;
        agvRouteRetrievalService.Setup(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<string?>())).ReturnsAsync(agvRoute);
        var action = AgvActionGenerator.GenerateMoveAction();

        await fixture.TestHarness.Bus.Publish(new ActionStarted(action.CorrelationId, agvCorrelationId));
        
        await fixture.ShouldHavePublishedAsync<ActionStarted>();
        await fixture.ShouldHaveConsumedAsync<ActionStarted>();
        agvRouteRetrievalService.Verify(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<string>()), Times.Once);
        fixture.PublisherMock.Verify(h => h.SendAgvRouteChangedAsync(agvRoute), Times.Once);
    }

    [TestMethod]
    public async Task Receiving_ActionUpdated_SendsAssignedActionGroupChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, new[] { "1", "2" }, []);
        var fixture = new ConsumerHubTestFixture<ActionStartedConsumer, IAgvPublisher>();
        Mock<IAgvRouteRetrievalService> agvRouteRetrievalService = fixture.AgvRouteServiceMock;
        agvRouteRetrievalService.Setup(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<string?>())).ReturnsAsync(agvRoute);
        var action = AgvActionGenerator.GenerateMoveAction();

        await fixture.TestHarness.Bus.Publish(new ActionUpdated(action.CorrelationId, agvCorrelationId));

        await fixture.ShouldHavePublishedAsync<ActionUpdated>();
        await fixture.ShouldHaveConsumedAsync<ActionUpdated>();
        agvRouteRetrievalService.Verify(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<string>()), Times.Once);
        fixture.PublisherMock.Verify(h => h.SendAgvRouteChangedAsync(agvRoute), Times.Once);
    }
}