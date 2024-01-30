using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Map;

public class NodeStateRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<INodeRepository> _nodeRepositoryMock = new();

    public NodeStateRequestHandlerTestFixture()
    {
        WithEmptyRepository();
        AddServices(s => s.AddTransient(_ => _nodeRepositoryMock.Object));
    }

    public NodeStateRequestHandlerTestFixture<TRequestClient, TConsumer> WithEmptyRepository()
    {
        _nodeRepositoryMock.Setup(r => r.GetNodeStatesForMapAsync(It.IsAny<Guid>())).ReturnsAsync((Guid correlationId) => throw new MapNotFoundException(nameof(correlationId), correlationId));
        return this;
    }

    public NodeStateRequestHandlerTestFixture<TRequestClient, TConsumer> WithNodeStates(ICollection<NodeLock> nodeStates)
    {
        _nodeRepositoryMock.Setup(r => r.GetNodeStatesForMapAsync(It.IsAny<Guid>())).ReturnsAsync(nodeStates);
        return this;
    }

    public new NodeStateRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}