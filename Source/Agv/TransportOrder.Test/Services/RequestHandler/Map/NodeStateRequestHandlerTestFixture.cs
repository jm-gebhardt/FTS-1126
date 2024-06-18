using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

public class NodeStateRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, MockConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<INodeRepository> _nodeRepositoryMock = new();
    
    public NodeStateRequestHandlerTestFixture<TRequestClient, TConsumer> WithEmptyRepository()
    {
        _nodeRepositoryMock.Setup(r => r.GetNodeStatesForMapAsync(It.IsAny<Guid>())).ReturnsAsync((Guid correlationId) => throw new MapNotFoundException(correlationId));
        return this;
    }

    public NodeStateRequestHandlerTestFixture<TRequestClient, TConsumer> WithNodeStates(ICollection<NodeLock> nodeStates)
    {
        _nodeRepositoryMock.Setup(r => r.GetNodeStatesForMapAsync(It.IsAny<Guid>())).ReturnsAsync(nodeStates);
        return this;
    }

    public new NodeStateRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        AddServices(s => s.AddTransient(_ => _nodeRepositoryMock.Object));
        base.Build();
        return this;
    }
}