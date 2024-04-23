using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

[TestClass]
public class MapPublisherTest
{
    [TestMethod]
    public async Task SendNodeStateChangedAsync_ValidData_NodeStateChangedSentToAllClients()
    {
        PublisherFixture<MapPublisher, MapHub, IMapClient> fixture = new PublisherFixture<MapPublisher, MapHub, IMapClient>().Build();
        var data = new NodeLock("NodeId", NodeLockState.Free, true, null, DateTime.UtcNow);
        var expectedDto = new NodeStateDto("NodeId", NodeLockState.Free, true, null);

        fixture.MapperMock.Setup(m => m.Map<NodeStateDto>(It.IsAny<NodeLock>())).Returns(expectedDto);

        await fixture.Publisher.SendNodeStateChangedAsync(data);

        fixture.HubContextMock.Verify(c => c.Clients.All.NodeStateChanged(It.IsAny<NodeStateDto>()), Times.Once);
    }
}