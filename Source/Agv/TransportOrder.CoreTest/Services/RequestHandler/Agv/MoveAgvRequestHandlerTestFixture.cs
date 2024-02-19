using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> BuildAsync(IBus? bus = null)
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _nodeStateRepository.Object);
        });

        if (bus != null)
        {
            AddServices(s => s.AddTransient(_ => bus));
        }
        else
        {
            var busMock = new Mock<IBus>();
            AddServices(s => s.AddTransient(_ => busMock.Object));
        }
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

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> WithNodeState(NodeLock? state)
    {
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(state);
        return this;
    }

    public void VerifyNodeWasReserved(string targetNodeId, Guid agvId)
    {
        _nodeStateRepository.Verify(
            x => x.ReserveNodeAsync(It.Is<string>(n => n == targetNodeId), It.Is<Guid>(a => a == agvId)), Times.Once());
    }
}