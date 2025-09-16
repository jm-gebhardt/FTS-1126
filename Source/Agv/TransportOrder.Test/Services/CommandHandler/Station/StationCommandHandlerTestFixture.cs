using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Station;

public class StationCommandHandlerTestFixture : MassTransitConsumerTestFixture<StationCommandHandler>
{
    public readonly Mock<INodeStateRepository> NodeStateRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IGraphRepository> _graphRepository = new();
    private readonly Mock<IMetricCollectionService> _metricCollectorServiceMock = new();

    public StationCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<StationCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => NodeStateRepository.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _graphRepository.Object);
        collection.AddTransient(_ => _metricCollectorServiceMock.Object);
        NodeStateRepository.Setup(x => x.FreeNodeAsync(It.IsAny<Guid>())).Verifiable();
        _stationRepository.Setup(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Station.Station>())).Verifiable();
        _metricCollectorServiceMock.Setup(x => x.CollectJamStateMetrics(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<bool>())).Verifiable();
        return this;
    }

    public StationCommandHandlerTestFixture WithNodeState(NodeLock? nodeState)
    {
        NodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<Guid>())).ReturnsAsync(nodeState);
        return this;
    }

    public StationCommandHandlerTestFixture WithStation(string nodeName)
    {
        var station = new TransportOrder.Domain.Models.Station.Station(nodeName, AgvGraph.NodeType.PickDrop, new List<AgvGraph.NodeType>());
        var id = station.Id;
        _stationRepository.Setup(x => x.GetStation(id)).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStation(nodeName.ToGuid())).ReturnsAsync(station);
        return this;
    }

    public StationCommandHandlerTestFixture WithLockableAndUnlockableNode(string nodeName)
    {
        NodeStateRepository.Setup(x => x.LockNodeAsync(nodeName.ToGuid())).ReturnsAsync(true);
        NodeStateRepository.Setup(x => x.UnlockNodeAsync(nodeName.ToGuid())).ReturnsAsync(true);
        return this;
    }

    public StationCommandHandlerTestFixture WithAgvsRelatedToNodes(ICollection<Guid> agvIds)
    {
        _graphRepository.Setup(x => x.GetAgvIdsRelatedToNodesAsync(It.IsAny<ICollection<GraphNode>>())).ReturnsAsync(agvIds);
        return this;
    }

    public StationCommandHandlerTestFixture WithAgvsGraph()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        _graphRepository.Setup(x => x.GetAgvGraphAsync(It.IsAny<Guid>())).ReturnsAsync(graph);
        return this;
    }

    public void VerifyStationUpdated(Times times)
    {
        _stationRepository.Verify(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Station.Station>()), times);
    }

    public void VerifyStationsAdded(Times times)
    {
        _stationRepository.Verify(x => x.AddStations(It.IsAny<AgvGraph>()), times);
    }

    public void VerifyMetricsCollected()
    {
        _metricCollectorServiceMock.Verify(x => x.CollectJamStateMetrics(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Once);
    }
}