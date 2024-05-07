using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Graphs;

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