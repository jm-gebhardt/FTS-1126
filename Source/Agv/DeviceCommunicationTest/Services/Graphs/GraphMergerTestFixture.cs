using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Requests;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Graphs;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Graphs;

internal class GraphMergerTestFixture : MassTransitConsumerTestFixture<GraphMerger>
{
    private Graph _gotGraph = null!;

    public GraphMergerTestFixture()
    {
        Build();
    }
    
    public AgvGraphReceived AgvGraphReceived { get; } = new(Guid.NewGuid(), "agv", AgvGraphGenerator.CreateDefaultGraph());

    protected override MassTransitConsumerTestFixture<GraphMerger> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton(RequestClientGenerator<GetGraph>.CreateDummyRequestClient(() => new GotGraph(_gotGraph)));
        return this;
    }

    public GraphMergerTestFixture WithCurrentGraph(Graph graph)
    {
        _gotGraph = graph;
        return this;
    }
}