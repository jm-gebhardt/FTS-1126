using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

public class AgvPositionChangedConsumerTestFixture : ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvPublisher>
{
    public AgvPositionChangedConsumerTestFixture WithNodeReserved(string nodeId, bool reserve = true)
    {
        NodeStateRepositoryMock.Setup(x => x.IsNodeReservedAsync(nodeId)).ReturnsAsync(reserve);
        NodeStateRepositoryMock.Setup(x => x.GetNodeStateAsync(nodeId)).ReturnsAsync(new NodeLock(nodeId, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
        return this;
    }

    public AgvPositionChangedConsumerTestFixture WithNodeOccupied()
    {
        NodeStateRepositoryMock.Setup(x => x.UnOccupyNodeAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        return this;
    }

    public AgvPositionChangedConsumerTestFixture WithOutNodeState()
    {
        NodeStateRepositoryMock.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync((NodeLock?)null);
        return this;
    }

    public AgvPositionChangedConsumerTestFixture WithAgvRoute(Guid agvCorrelationId, string lastNodeId, CurrentAgvRouteReadModel? currentAgvRoute)
    {
        AgvRouteServiceMock.Setup(x => x.GetAgvRouteAsync(agvCorrelationId, lastNodeId)).ReturnsAsync(currentAgvRoute);
        return this;
    }
}