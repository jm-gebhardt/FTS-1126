using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

internal class AgvGraphReceivedEventHandlerTestFixture : MassTransitConsumerTestFixture<AgvGraphReceivedEventHandler>
{
    private Mock<IGraphRepository> _graphRepositoryMock = null!;

    public AgvGraphReceived AgvGraphReceived { get; } = new(Guid.NewGuid(), "agv", AgvGraphGenerator.CreateDefaultGraph());

    public AgvGraphReceivedEventHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvGraphReceivedEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        _graphRepositoryMock = new Mock<IGraphRepository>();
        collection.AddSingleton(_graphRepositoryMock.Object);
        collection.AddSingleton(Mock.Of<IMapRepository>());
        return this;
    }

    public AgvGraphReceivedEventHandlerTestFixture WithGraphRepositoryAddOrUpdateAgvGraphAsyncReturns(bool result)
    {
        _graphRepositoryMock.Setup(r => r.AddOrUpdateAgvGraphAsync(It.Is<AgvGraph>(x => x.CorrelationId == AgvGraphReceived.Graph.CorrelationId)))
            .ReturnsAsync(result);

        return this;
    }
}