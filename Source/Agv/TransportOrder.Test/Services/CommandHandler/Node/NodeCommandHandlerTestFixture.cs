using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Node;

public class NodeCommandHandlerTestFixture : MassTransitConsumerTestFixture<NodeCommandHandler>
{
    public readonly Mock<INodeStateRepository> NodeStateRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IGraphRepository> _graphRepository = new();

    public NodeCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<NodeCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => NodeStateRepository.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _queueRepository.Object);
        collection.AddTransient(_ => _graphRepository.Object);
        NodeStateRepository.Setup(x => x.FreeNodeAsync(It.IsAny<string>())).Verifiable();

        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeState(NodeLock? nodeState)
    {
        NodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(nodeState);
        return this;
    }

    public NodeCommandHandlerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueForAgv(It.IsAny<Guid>())).ReturnsAsync(queue);
        return this;
    }

    public NodeCommandHandlerTestFixture WithStation(string nodeId)
    {
        var station = new TransportOrder.Domain.Models.Station.Station(nodeId, AgvGraph.NodeType.PickDrop, new List<AgvGraph.NodeType>());
        var id = station.Id;
        _stationRepository.Setup(x => x.GetStation(id)).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStation(nodeId)).ReturnsAsync(station);
        return this;
    }

    public NodeCommandHandlerTestFixture WithAnyStation()
    {
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<string>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeReserved(string nodeId, bool reserve = true)
    {
        NodeStateRepository.Setup(x => x.OccupyNodeAsync(nodeId, It.IsAny<Guid>())).Verifiable();
        NodeStateRepository.Setup(x => x.IsNodeReservedAsync(nodeId)).ReturnsAsync(reserve);
        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeOccupied(Guid agvId)
    {
        NodeStateRepository.Setup(x => x.UnOccupyNodeAsync(agvId)).ReturnsAsync(true);
        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeStates(ICollection<NodeLock> nodeLocks)
    {
        NodeStateRepository.Setup(x => x.GetNodeLocksForAgv(It.IsAny<Guid>())).ReturnsAsync(nodeLocks);
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
}