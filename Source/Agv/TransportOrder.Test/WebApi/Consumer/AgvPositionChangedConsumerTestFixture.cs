using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

public class AgvPositionChangedConsumerTestFixture : ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvPublisher>
{
    public AgvPositionChangedConsumerTestFixture WithNodeStates(NodeLock? oldNodeState, NodeLock? newNodeState)
    {
        NodeRepositoryMock.SetupSequence(x => x.GetNodeAsync(It.IsAny<Guid>()))
            .ReturnsAsync(oldNodeState != null ? new GraphNode { Id = oldNodeState.NodeId, Lock = oldNodeState } : throw new NodeNotFoundException(""))
            .ReturnsAsync(newNodeState != null ? new GraphNode { Id = newNodeState.NodeId, Lock = newNodeState } : throw new NodeNotFoundException(""));
        return this;
    }

    public AgvPositionChangedConsumerTestFixture WithAgvRoute(Guid agvCorrelationId, string lastNodeId, CurrentAgvRouteReadModel? currentAgvRoute)
    {
        AgvRouteServiceMock.Setup(x => x.GetAgvRouteAsync(agvCorrelationId, lastNodeId.ToGuid())).ReturnsAsync(currentAgvRoute);
        return this;
    }
}