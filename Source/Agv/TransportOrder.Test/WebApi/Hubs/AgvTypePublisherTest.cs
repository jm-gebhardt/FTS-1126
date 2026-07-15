using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.AgvType;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

[TestClass]
public class AgvTypePublisherTest
{
    [TestMethod]
    public async Task SendAgvTypeAcknowledged_ValidData_BroadcastsAgvTypeAcknowledged()
    {
        PublisherFixture<AgvTypePublisher, AgvTypeHub, IAgvTypeClient> fixture = new PublisherFixture<AgvTypePublisher, AgvTypeHub, IAgvTypeClient>().Build();
        var id = Guid.NewGuid();

        await fixture.Publisher.SendAgvTypeAcknowledged(id);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvTypeAcknowledged(It.Is<AgvTypeAcknowledgedDto>(dto => dto.Id == id)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvTypeCreated_ValidData_BroadcastsAgvTypeCreated()
    {
        PublisherFixture<AgvTypePublisher, AgvTypeHub, IAgvTypeClient> fixture = new PublisherFixture<AgvTypePublisher, AgvTypeHub, IAgvTypeClient>().Build();
        var agvType = new AgvTypeDto { Id = Guid.NewGuid() };

        await fixture.Publisher.SendAgvTypeCreated(agvType);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvTypeCreated(It.Is<AgvTypeDto>(dto => dto == agvType)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvTypeUpdated_ValidData_BroadcastsAgvTypeUpdated()
    {
        PublisherFixture<AgvTypePublisher, AgvTypeHub, IAgvTypeClient> fixture = new PublisherFixture<AgvTypePublisher, AgvTypeHub, IAgvTypeClient>().Build();
        var agvType = new AgvTypeDto { Id = Guid.NewGuid() };

        await fixture.Publisher.SendAgvTypeUpdated(agvType);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).AgvTypeUpdated(It.Is<AgvTypeUpdatedDto>(dto => dto.AgvType == agvType)), Times.Once);
    }
}
