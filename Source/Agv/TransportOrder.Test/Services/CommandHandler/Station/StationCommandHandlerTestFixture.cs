using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Station;

public class StationCommandHandlerTestFixture : MassTransitConsumerTestFixture<StationCommandHandler>
{
    public readonly Mock<INodeStateRepository> NodeStateRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();

    public StationCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<StationCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => NodeStateRepository.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        NodeStateRepository.Setup(x => x.FreeNodeAsync(It.IsAny<string>())).Verifiable();
       
        return this;
    }

    public StationCommandHandlerTestFixture WithNodeState(NodeLock? nodeState)
    {
        NodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(nodeState);
        return this;
    }

    public StationCommandHandlerTestFixture WithStation(string nodeId)
    {
        var station = new TransportOrder.Domain.Models.Station.Station(nodeId, AgvGraph.NodeType.PickDrop, new List<AgvGraph.NodeType>());
        var id = station.Id;
        _stationRepository.Setup(x => x.GetStation(id)).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStation(nodeId)).ReturnsAsync(station);
        return this;
    }

    public StationCommandHandlerTestFixture WithAnyStation()
    {
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<string>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        return this;
    }

    public StationCommandHandlerTestFixture WithLockableAndUnlockableNode(string nodeId)
    {
        NodeStateRepository.Setup(x => x.LockNodeAsync(nodeId)).ReturnsAsync(true);
        NodeStateRepository.Setup(x => x.UnlockNodeAsync(nodeId)).ReturnsAsync(true);
        return this;
    }
}