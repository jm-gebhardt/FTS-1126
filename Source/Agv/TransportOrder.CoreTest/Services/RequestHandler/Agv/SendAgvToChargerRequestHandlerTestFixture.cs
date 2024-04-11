using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class SendAgvToChargerRequestHandlerTestFixture : MassTransitConsumerTestFixture<SendAgvToChargerRequestHandler>
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();

    public SendAgvToChargerRequestHandlerTestFixture()
    {
        Build();
    }
    
    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    protected override MassTransitConsumerTestFixture<SendAgvToChargerRequestHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _nodeStateRepository.Object);
        collection.AddTransient(_ => ChargingPointManager.Object);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvsToMockRepository(agv);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithCurrentAction(AgvAction? action)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(action);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture ReserveChargerReturns(GraphNode chargingNode)
    {
        ChargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<string?>())).ReturnsAsync(chargingNode);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithNodeLock(NodeLock nodeLock)
    {
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(nodeLock);
        return this;
    }
}