using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Node;

public class NodeCommandHandlerTestFixture: MassTransitConsumerTestFixture<NodeCommandHandler>
{
    public readonly Mock<INodeStateRegistry> NodeStateRegistry = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => NodeStateRegistry.Object);
        NodeStateRegistry.Setup(x => x.FreeNodeAsync(It.IsAny<string>())).Verifiable();
    }
}