using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

public class MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IRestrictedAreaRepository> _restrictedAreaRepository = new();

    public Mock<IParkPointService> ParkPointService { get; } = new();

    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    public new MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> BuildAsync()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _nodeStateRepository.Object);
            s.AddTransient(_ => _stationRepository.Object);
            s.AddTransient(_ => ParkPointService.Object);
            s.AddTransient(_ => ChargingPointManager.Object);
            s.AddTransient(_ => _restrictedAreaRepository.Object);
        });

        AddConsumers<SuccessQueueActionGroupRequestHandler>();
        _nodeStateRepository.Setup(x => x.ReserveNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).Verifiable();
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.Is<string>(y => y == "target"))).ReturnsAsync(new TransportOrder.Domain.Models.Node.NodeLock("target", TransportOrder.Domain.Models.Node.NodeLockState.Free, false, Guid.NewGuid(), DateTime.UtcNow));
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<string>())).ReturnsAsync(new Station());
        base.Build();
        return this;
    }

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDb.AddAgvs(agvs);
        return this;
    }

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> WithNode(GraphNode? node)
    {
        _agvDb.Setup(x => x.Graphs.GetNodeAsync(It.IsAny<string>())).ReturnsAsync((string id) => node ?? throw new NodeNotFoundException(id));
        return this;
    }

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> WithInsideRestrictedArea(bool isInside)
    {
        _restrictedAreaRepository.Setup(x => x.IsPointInRestrictedArea(It.IsAny<float>(), It.IsAny<float>())).ReturnsAsync(isInside);
        return this;
    }

    public void VerifyNodeWasReserved(GraphNode targetNode, Guid agvId)
    {
        switch (targetNode.Type)
        {
            case AgvGraph.NodeType.Charge:
                ChargingPointManager.Verify(x => x.ReserveChargerAsync(It.Is<GraphNode>(n => n == targetNode), It.Is<Guid>(a => a == agvId)), Times.Once());
                break;

            case AgvGraph.NodeType.Park:
                ParkPointService.Verify(x => x.ReserveParkAsync(It.Is<string>(n => n == targetNode.Id), It.Is<Guid>(a => a == agvId)), Times.Once());
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}