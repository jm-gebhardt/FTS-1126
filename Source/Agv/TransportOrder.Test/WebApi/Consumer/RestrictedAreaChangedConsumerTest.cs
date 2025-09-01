using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Events.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.RestrictedArea;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class RestrictedAreaChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_RestrictedAreaUpdated_ValidData_SendsRestrictedAreaChanged()
    {
        var fixture = new ConsumerHubTestFixture<RestrictedAreaChangedConsumer, IRestrictedAreaPublisher>();

        var restrictedArea = new RestrictedArea{Id = Guid.NewGuid(), Area = [new RestrictedAreaPolygonPoint(1,1,0, Guid.NewGuid())] };
        await fixture.PublishMessageAsync(new RestrictedAreaUpdated(restrictedArea));

        fixture.PublisherMock.Verify(h => h.SendRestrictedAreaChangedAsync(restrictedArea), Times.Once);
    }

    [TestMethod]
    public async Task Consume_RestrictedAreaDeleted_ValidData_SendsRestrictedAreaChanged()
    {
        var fixture = new ConsumerHubTestFixture<RestrictedAreaChangedConsumer, IRestrictedAreaPublisher>();

        var restrictedArea = Guid.NewGuid();

        await fixture.PublishMessageAsync(new RestrictedAreaDeleted(restrictedArea));

        fixture.PublisherMock.Verify(h => h.SendRestrictedAreaDeletedAsync(restrictedArea), Times.Once);
    }
}