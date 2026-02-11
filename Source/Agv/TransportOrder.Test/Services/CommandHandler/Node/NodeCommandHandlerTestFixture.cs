using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Node;

public class NodeCommandHandlerTestFixture : MassTransitConsumerTestFixture<NodeCommandHandler>
{
    public readonly Mock<INodeRepository> NodeRepository = new();
    public readonly Mock<IStationRepository> StationRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    public NodeCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<NodeCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => NodeRepository.Object);
        collection.AddTransient(_ => StationRepository.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _queueRepository.Object);
        collection.AddTransient(_ => _mapRepository.Object);
        collection.AddTransient(_ => _agvDb.Object);
        NodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(true).Verifiable();
        StationRepository.Setup(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Station.Station>())).Returns(Task.CompletedTask).Verifiable();
        _agvDb.Setup(x => x.Graphs.UpdateNode(It.IsAny<GraphNode>())).Verifiable();
        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeLock(NodeLock? nodeLock)
    {
        NodeRepository.Setup(x => x.GetNodeAsync(It.IsAny<Guid>())).ReturnsAsync(() =>
        {
            var node = new GraphNode(nodeLock?.NodeId?? Guid.NewGuid(), "")
            {
                Lock = nodeLock
            };
            return node;
        });
        return this;
    }

    public NodeCommandHandlerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueForAgv(It.IsAny<Guid>())).ReturnsAsync(queue);
        return this;
    }

    public NodeCommandHandlerTestFixture WithStation(string nodeName)
    {
        var station = new TransportOrder.Domain.Models.Station.Station(nodeName, AgvGraph.NodeType.PickDrop, new List<AgvGraph.NodeType>());
        return WithStation(station);
    }

    public NodeCommandHandlerTestFixture WithStation(TransportOrder.Domain.Models.Station.Station station)
    {
        StationRepository.Setup(x => x.GetStation(station.Id)).ReturnsAsync(station);
        StationRepository.Setup(x => x.GetStation(station.Name.ToGuid())).ReturnsAsync(station);
        return WithNodeLock(station.NodeLock);
    }

    public NodeCommandHandlerTestFixture WithAnyStation()
    {
        StationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeReserved(bool reserve = true)
    {
        NodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(reserve).Verifiable();
        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeOccupied()
    {
        NodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(true);
        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeLocks(ICollection<NodeLock> nodeLocks)
    {
        NodeRepository.Setup(x => x.GetNodesForAgv(It.IsAny<Guid>())).ReturnsAsync((Guid _) =>
        {
            List<GraphNode> nodes = [];
            foreach (var nodeLock in nodeLocks)
            {
                var node = new GraphNode()
                {
                    Lock = nodeLock,
                    Id = nodeLock.NodeId
                };
                nodes.Add(node);
            }
            return nodes;
        });
       
        return this;
    }

    public NodeCommandHandlerTestFixture WithActionGroups(ICollection<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetPendingActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups);
        return this;
    }

    public NodeCommandHandlerTestFixture WithCurrentAgvAction(AgvAction action)
    {
        _actionGroupRepository
            .Setup(r => r.GetCurrentAgvActionAsync(It.IsAny<Guid>()))
            .ReturnsAsync(action);
        return this;
    }

    public NodeCommandHandlerTestFixture WithNode(GraphNode node)
    {
        _agvDb.AddNode(node);
        _mapRepository.Setup(x => x.GetMapIdForNodeAsync(node.Id)).ReturnsAsync(Guid.NewGuid);
        return this;
    }

    public void VerifyNodeUpdated()
    {
        _agvDb.Verify(x => x.Graphs.UpdateNode(It.IsAny<GraphNode>()), Times.Once);
    }
}