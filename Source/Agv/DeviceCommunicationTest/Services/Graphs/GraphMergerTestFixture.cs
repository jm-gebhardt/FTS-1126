using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Graphs;

internal class GraphMergerTestFixture : MassTransitConsumerTestFixture<GraphMerger>
{
    private Graph _gotGraph = null!;

    public InternalAgvGraphReceived InternalAgvGraphReceived { get; } = new(Guid.NewGuid(), "agv", AgvGraphGenerator.CreateDefaultGraph());

    protected override MassTransitConsumerTestFixture<GraphMerger> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton(RequestClientGenerator<GetGraph>.CreateDummyRequestClient(_ => new GotGraph(_gotGraph)));
        return this;
    }

    public GraphMergerTestFixture WithSuccessfulUpdateGraphResponse(Func<UpdateGraphRequest, Success> callback)
    {
        AddServices(s => s.AddSingleton(RequestClientGenerator<UpdateGraphRequest>.CreateDummyRequestClient(callback)));
        return this;
    }

    public GraphMergerTestFixture WithCurrentGraph(Graph graph)
    {
        _gotGraph = graph;
        return this;
    }

    [PublicAPI]
    public new GraphMergerTestFixture Build()
    {
        base.Build();
        return this;
    }
}