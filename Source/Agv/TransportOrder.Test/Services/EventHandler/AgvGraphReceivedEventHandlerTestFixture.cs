using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

internal class AgvGraphReceivedEventHandlerTestFixture : MassTransitConsumerTestFixture<AgvGraphReceivedEventHandler>
{
    private Mock<IGraphRepository> _graphRepositoryMock = null!;
    private Mock<IStationRepository> _stationRepositoryMock = null!;

    public AgvGraphReceived AgvGraphReceived { get; } = new(Guid.NewGuid(), "agv", AgvGraphGenerator.CreateDefaultGraph());

    public AgvGraphReceivedEventHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvGraphReceivedEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _stationRepositoryMock = new Mock<IStationRepository>();
        _stationRepositoryMock.Setup(x => x.AddStations(It.IsAny<AgvGraph>())).Verifiable();
        collection.AddSingleton(_graphRepositoryMock.Object);
        collection.AddSingleton(_stationRepositoryMock.Object);
        collection.AddSingleton(Mock.Of<IMapRepository>());
        return this;
    }

    public AgvGraphReceivedEventHandlerTestFixture WithGraphRepositoryAddOrUpdateAgvGraphAsyncReturns((bool AnyRelationsMissing, bool AnyChanges) result)
    {
        _graphRepositoryMock.Setup(r => r.AddOrUpdateAgvGraphAsync(It.Is<AgvGraph>(x => x.CorrelationId == AgvGraphReceived.Graph.CorrelationId)))
            .ReturnsAsync(result);

        return this;
    }

    public void VerifyStationsAdded(Times times)
    {
        _stationRepositoryMock.Verify(x => x.AddStations(It.IsAny<AgvGraph>()), times);
    }
}