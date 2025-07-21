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

        const string nodeId = "NodeId";
        var stationId = Guid.NewGuid();
        var modifiedDate = DateTime.UtcNow;
        var data = new NodeLock(nodeId, NodeLockState.Free, true, null, modifiedDate);
        var expectedDto = new NodeStateDto(nodeId, NodeLockState.Free, true, null, modifiedDate, stationId);

        fixture.MapperMock.Setup(m => m.Map<NodeStateDto>(It.IsAny<NodeLock>())).Returns(expectedDto);

        await fixture.Publisher.SendNodeStateChangedAsync(data, stationId, null, null, null);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).NodeStateChanged(It.IsAny<NodeStateDto>()), Times.Once);
    }
}