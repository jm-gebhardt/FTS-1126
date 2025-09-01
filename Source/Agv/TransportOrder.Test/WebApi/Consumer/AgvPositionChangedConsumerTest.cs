using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class AgvPositionChangedConsumerTest
{
    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    public async Task Receiving_AgvPositionChanged_SendsAgvPosition()
    {
        var fixture = new AgvPositionChangedConsumerTestFixture();
        await fixture.TestHarness.Bus.Publish(
            new AgvPositionChanged(
                AgvSerialNumber.ToGuid(),
                new AgvPosition(false, 0, 0, 0, 0),
                null,
                AgvSerialNumber
            ));
        
        await fixture.ShouldHavePublishedAsync<AgvPositionChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvPositionChanged>();
        fixture.PublisherMock.Verify(h => h.SendAgvPositionAsync(It.Is<AgvPositionStatus>(o => o.SerialNumber == AgvSerialNumber)), Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_AgvRouteIsNull_SendAgvRouteAsyncNotCalled()
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(null, null)
            .WithAgvRoute(AgvSerialNumber.ToGuid(), "new", null);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));
        
        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        fixture.PublisherMock.Verify(r => r.SendAgvRouteChangedAsync(It.IsAny<CurrentAgvRouteReadModel>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_AgvRouteIsNotNull_SendAgvRouteAsyncCalled()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, NodeGenerator.GenerateRouteNodes(), []);
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(null, null)
            .WithAgvRoute(agvCorrelationId, "new", agvRoute);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                agvCorrelationId,
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();

        fixture.AgvRouteServiceMock.Verify(r => r.GetAgvRouteAsync(agvCorrelationId, "new"), Times.Once);
        fixture.PublisherMock.Verify(r => r.SendAgvRouteChangedAsync(agvRoute), Times.Once);
    }
}