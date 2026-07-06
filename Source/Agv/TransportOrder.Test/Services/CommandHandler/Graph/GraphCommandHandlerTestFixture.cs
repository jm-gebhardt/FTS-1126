using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using IStationRepository = Gebhardt.Acc.TransportOrder.Domain.Repositories.IStationRepository;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Graph;

public class GraphCommandHandlerTestFixture : MassTransitConsumerTestFixture<GraphCommandHandler>
{
    public readonly Mock<IGraphRepository> GraphRepository = new();
    public readonly Mock<IStationRepository> StationRepository = new();

    public GraphCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<GraphCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton(Mock.Of<ILogger<GraphCommandHandler>>());
         collection.AddSingleton(GraphRepository.Object);
        collection.AddSingleton(StationRepository.Object);
        collection.AddTransient(_ => new Mock<ILoggingService>().Object);
        GraphRepository.Setup(x => x.UpdateNode(It.IsAny<GraphNode>())).Verifiable();
        StationRepository.Setup(x => x.AddStations(It.IsAny<IGraph>())).Verifiable();
        return this;
    }
}