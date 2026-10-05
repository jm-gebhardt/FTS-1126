using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class NodeTestFixture : TransportOrderDbFixture
{
    private EfGraphRepository _graphRepository = null!;

    public async Task<HashSet<GraphNode>> AddNodesAsync(ActionGroup actionGroup, bool addStraightAway)
    {
        var nodes = new HashSet<GraphNode>();
        foreach (var action in actionGroup.Actions)
        {
            if (action.Target.Node is not null)
            {
                nodes.Add(action.Target.Node);
            }
            nodes.AddRange(action.Nodes);
        }
        await AddNodesAsync(nodes.ToList(), addStraightAway);
        return nodes;
    }

    public async Task AddNodesAsync(List<GraphNode> nodes, bool addStraightAway = true)
    {
        var existingNodes = await _graphRepository.GetNodesByIdsAsync(nodes.Select(n => n.Id).ToList());

        foreach (var existingNode in existingNodes)
        {
            var en = nodes.Single(n => n.Id == existingNode.Id);
            nodes.Remove(en);
        }
        var graph = new Graph { Nodes = nodes.ToArray() };

        if (addStraightAway)
        {
            await _graphRepository.UpdateGraphAsync(graph);
        }
        else
        {
            AddInitTask(_graphRepository.UpdateGraphAsync(graph));
        }
    }

    protected override void Build()
    {
        base.Build();

        _graphRepository = new EfGraphRepository(ContextFactory,
            Mapper,
            new SemaphoreProvider<IGraphRepositorySemaphore>(Mock.Of<ILogger<SemaphoreProvider<IGraphRepositorySemaphore>>>()),
            Mock.Of<ILogger<EfGraphRepository>>(), Mock.Of<ILoggingService>());
    }
}