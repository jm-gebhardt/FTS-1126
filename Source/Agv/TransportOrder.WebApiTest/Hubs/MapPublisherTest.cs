using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs;

[TestClass]
public class MapPublisherTest
{
    [TestMethod]
    public async Task SendNodeStateChangedAsync_ValidData_NodeStateChangedSentToAllClients()
    {
        PublisherFixture<MapPublisher, MapHub, IMapClient> fixture = new PublisherFixture<MapPublisher, MapHub, IMapClient>()
            .WithMapper(new MapProfile(), new DateTimeProfile())
            .Build();

        var nodeState = new NodeLock("NodeId", NodeLockState.Free, true, null, DateTime.UtcNow);
        await fixture.Publisher.SendNodeStateChangedAsync(nodeState);
        fixture.HubContextMock.Verify( c => c.Clients.All.NodeStateChanged(It.Is<NodeStateDto>(n => n.NodeId == nodeState.NodeId)), Times.Once);
    }
}