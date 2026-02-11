using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

[TestClass]
public class MapPublisherTest
{
    [TestMethod]
    public async Task SendNodeStateChangedAsync_ValidData_NodeStateChangedSentToAllClients()
    {
        PublisherFixture<MapPublisher, MapHub, IMapClient> fixture = new PublisherFixture<MapPublisher, MapHub, IMapClient>().Build();

        const string nodeName = "NodeId";
        var stationId = Guid.NewGuid();
        var modifiedDate = DateTime.UtcNow;
        var data = new NodeLock(nodeName.ToGraphNode(), NodeLockState.Free, true, null, modifiedDate);
        var expectedDto = new NodeStateDto(nodeName, NodeLockState.Free, true, null, modifiedDate, stationId);

        fixture.MapperMock.Setup(m => m.Map<NodeStateDto>(It.IsAny<NodeLock>())).Returns(expectedDto);

        await fixture.Publisher.SendNodeStateChangedAsync(data, nodeName, stationId, null, null, null);

        fixture.HubContextMock.Verify(c => c.Clients.Client(fixture.TestConnectionId).NodeStateChanged(It.IsAny<NodeStateDto>()), Times.Once);
    }
}