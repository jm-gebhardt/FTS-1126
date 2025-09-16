using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

public class SendAgvToChargerRequestHandlerTestFixture : MassTransitConsumerTestFixture<SendAgvToChargerRequestHandler>
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();

    public SendAgvToChargerRequestHandlerTestFixture()
    {
        Build();
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new Station());
    }

    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    protected override MassTransitConsumerTestFixture<SendAgvToChargerRequestHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _nodeStateRepository.Object);
        collection.AddTransient(_ => ChargingPointManager.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvs(agv);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithCurrentAction(AgvAction? action)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(action);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture ReserveChargerReturns(GraphNode chargingNode)
    {
        ChargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<Guid?>())).ReturnsAsync(chargingNode);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithNodeLock(NodeLock nodeLock)
    {
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<Guid>())).ReturnsAsync(nodeLock);
        return this;
    }
}