using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

public class AgvPositionChangedConsumerTestFixture : ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvPublisher>
{
    public AgvPositionChangedConsumerTestFixture WithNodeStates(NodeLock? oldNodeState, NodeLock? newNodeState)
    {
        NodeStateRepositoryMock.SetupSequence(x => x.GetNodeStateAsync(It.IsAny<string>()))
            .ReturnsAsync(oldNodeState)
            .ReturnsAsync(newNodeState)
            .ReturnsAsync(oldNodeState);
        return this;
    }

    public AgvPositionChangedConsumerTestFixture WithAgvRoute(Guid agvCorrelationId, string lastNodeId, CurrentAgvRouteReadModel? currentAgvRoute)
    {
        AgvRouteServiceMock.Setup(x => x.GetAgvRouteAsync(agvCorrelationId, lastNodeId)).ReturnsAsync(currentAgvRoute);
        return this;
    }
}