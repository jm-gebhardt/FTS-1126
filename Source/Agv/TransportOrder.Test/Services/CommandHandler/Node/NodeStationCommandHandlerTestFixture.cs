using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Node;

public class NodeStationCommandHandlerTestFixture : MassTransitConsumerTestFixture<NodeStationCommandHandler>
{
    private readonly Mock<INodeRepository> _nodeRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly Mock<IGraphRepository> _graphRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<ILoggingService> _loggingService = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    public NodeStationCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<NodeStationCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _nodeRepository.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _queueRepository.Object);
        collection.AddTransient(_ => _mapRepository.Object);
        collection.AddTransient(_ => _graphRepository.Object);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _loggingService.Object);
        collection.AddTransient(_ => _logEntryRepository.Object);

        _nodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(true).Verifiable();
        _stationRepository.Setup(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Station.Station>())).Returns(Task.CompletedTask).Verifiable();
        _agvDb.Setup(x => x.Graphs.UpdateNode(It.IsAny<GraphNode>())).Verifiable();
        _loggingService
            .Setup(x => x.LogStation(It.IsAny<ILogger>(), It.IsAny<LogLevel>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>?>()))
            .Returns(Task.CompletedTask);
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithNodeLock(NodeLock? nodeLock)
    {
        _nodeRepository.Setup(x => x.GetNodeAsync(It.IsAny<Guid>())).ReturnsAsync(() =>
        {
            var node = new GraphNode(nodeLock?.NodeId?? Guid.NewGuid(), "")
            {
                Lock = nodeLock
            };
            return node;
        });
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueForAgv(It.IsAny<Guid>())).ReturnsAsync(queue);
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithStation(string nodeName)
    {
        var station = new TransportOrder.Domain.Models.Station.Station(nodeName, AgvGraph.NodeType.PickDrop, new List<AgvGraph.NodeType>());
        return WithStation(station);
    }

    public NodeStationCommandHandlerTestFixture WithStation(TransportOrder.Domain.Models.Station.Station station)
    {
        _stationRepository.Setup(x => x.GetStation(station.Id)).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStation(station.Name.ToGuid())).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStations(It.IsAny<Guid?>())).ReturnsAsync([station]);
        _stationRepository.Setup(x => x.GetStations()).ReturnsAsync([station]);
        return WithNodeLock(station.NodeLock);
    }

    public NodeStationCommandHandlerTestFixture WithAnyStation()
    {
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithNodeReserved(bool reserve = true)
    {
        _nodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(reserve).Verifiable();
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithNodeOccupied()
    {
        _nodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(true);
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithNodeLocks(ICollection<NodeLock> nodeLocks)
    {
        _nodeRepository.Setup(x => x.GetNodesForAgv(It.IsAny<Guid>())).ReturnsAsync((Guid _) =>
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

    public NodeStationCommandHandlerTestFixture WithActionGroups(ICollection<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetPendingActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups);
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithCurrentAgvAction(AgvAction action)
    {
        _actionGroupRepository
            .Setup(r => r.GetCurrentAgvActionAsync(It.IsAny<Guid>()))
            .ReturnsAsync(action);
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithNode(GraphNode node)
    {
        _agvDb.AddNode(node);
        _mapRepository.Setup(x => x.GetMapIdForNodeAsync(node.Id)).ReturnsAsync(Guid.NewGuid);
        _nodeRepository.Setup(x => x.GetNodeAsync(node.Id)).ReturnsAsync(node);
        _nodeRepository.Setup(x => x.GetNodeAsync(It.IsAny<Guid>())).ReturnsAsync(node);
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithAgvsRelatedToNodes(ICollection<Guid> agvIds)
    {
        _graphRepository.Setup(x => x.GetAgvIdsRelatedToNodesAsync(It.IsAny<ICollection<GraphNode>>())).ReturnsAsync(agvIds);
        return this;
    }

    public NodeStationCommandHandlerTestFixture WithAgvsGraph()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        _graphRepository.Setup(x => x.GetAgvGraphAsync(It.IsAny<Guid>())).ReturnsAsync(graph);
        return this;
    }

    public void VerifyNodeUpdated()
    {
        _agvDb.Verify(x => x.Graphs.UpdateNode(It.IsAny<GraphNode>()), Times.Once);
    }

    public void VerifyNodeLockUpdated(Times times)
    {
        _nodeRepository.Verify(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>()), times);
    }
    
    public void VerifyStationUpdated(Times times)
    {
        _stationRepository.Verify(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Station.Station>()), times);
    }
    
    public void VerifyStationsAdded(Times times)
    {
        _stationRepository.Verify(x => x.AddStations(It.IsAny<AgvGraph>()), times);
    }
}