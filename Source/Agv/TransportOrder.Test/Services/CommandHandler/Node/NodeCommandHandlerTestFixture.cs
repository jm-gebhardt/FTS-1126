using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.CommandHandler.Node;

public class NodeCommandHandlerTestFixture : MassTransitConsumerTestFixture<NodeCommandHandler>
{
    public readonly Mock<INodeStateRepository> NodeStateRepository = new();

    public NodeCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<NodeCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => NodeStateRepository.Object);
        NodeStateRepository.Setup(x => x.FreeNodeAsync(It.IsAny<string>())).Verifiable();
        return this;
    }

    public NodeCommandHandlerTestFixture WithNodeState(NodeLock? nodeState)
    {
        NodeStateRepository.Setup(x => x.GetNodeStateAsync(It.IsAny<string>())).ReturnsAsync(nodeState);
        return this;
    }
}