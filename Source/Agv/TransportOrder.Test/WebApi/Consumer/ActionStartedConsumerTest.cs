using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class ActionStartedConsumerTest
{
    [TestMethod]
    public async Task Receiving_ActionStarted_SendsAssignedActionGroupChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, new[] { "1", "2" });
        var fixture = new ConsumerHubTestFixture<ActionStartedConsumer, IAgvPublisher>();
        Mock<IAgvRouteRetrievalService> agvRouteRetrievalService = fixture.AgvRouteServiceMock;
        agvRouteRetrievalService.Setup(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<string?>())).ReturnsAsync(agvRoute);
        var action = AgvActionGenerator.GenerateMoveAction();

        await fixture.TestHarness.Bus.Publish(new ActionStarted(action.CorrelationId, agvCorrelationId));

        (await fixture.TestHarness.Published.Any<ActionStarted>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<ActionStarted>()).ShouldBeTrue();
        agvRouteRetrievalService.Verify(m => m.GetAgvRouteAsync(agvCorrelationId, It.IsAny<string>()), Times.Once);
        fixture.PublisherMock.Verify(h => h.SendAgvRouteChangedAsync(agvRoute), Times.Once);
    }
}