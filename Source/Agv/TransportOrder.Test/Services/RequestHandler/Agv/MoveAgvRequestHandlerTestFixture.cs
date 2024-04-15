using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.Agv;

public class MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();

    public Mock<IParkPointService> ParkPointService { get; } = new();

    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    public new MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> BuildAsync()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _nodeStateRepository.Object);
            s.AddTransient(_ => ParkPointService.Object);
            s.AddTransient(_ => ChargingPointManager.Object);
        });

        AddConsumers<SuccessQueueActionGroupRequestHandler>();
        _nodeStateRepository.Setup(x => x.ReserveNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).Verifiable();
        base.Build();
        return this;
    }

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> AddAgvs(ICollection<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDb.Setup(x => x.Agvs.ThrowIfAgvNotExistsAsync(It.IsAny<Guid>())).Callback((Guid c) =>
        {
            if (agvs.FirstOrDefault(a => a.CorrelationId == c) != null)
            {
                return;
            }
            throw new AgvNotFoundException(c);
        });
        return this;
    }

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> WithNode(GraphNode? node)
    {
        _agvDb.Setup(x => x.Graphs.GetNodeAsync(It.IsAny<string>())).ReturnsAsync((string id) => node ?? throw new NodeNotFoundException(id));
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