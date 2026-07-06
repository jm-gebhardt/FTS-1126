using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Map;

public class MapCommandHandlerBusTestFixture : MassTransitConsumerTestFixture<MapCommandHandler>
{
    public readonly Mock<IMapRepository> MapRepositoryMock = new();

    public readonly Mock<IGraphRepository> GraphRepositoryMock = new();

    public MapCommandHandlerBusTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<MapCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton(MapRepositoryMock.Object);
        collection.AddSingleton(GraphRepositoryMock.Object);
        collection.AddSingleton(Mock.Of<ILogger<MapCommandHandler>>());
        return this;
    }
}
