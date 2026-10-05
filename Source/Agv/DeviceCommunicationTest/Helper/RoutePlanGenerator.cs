using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunicationTest.Helper;

[PublicAPI]
public static class RoutePlanGenerator
{
    public static RoutePlan Build(IReadOnlyList<GraphNode> nodes, IReadOnlyList<Guid> edgeIds)
    {
        var baseEdges = Enumerable.Range(0, nodes.Count - 1)
            .Select(i => new GraphEdge(nodes[i].Id, nodes[i + 1].Id, 1f, edgeIds[i]))
            .ToList();
        return new RoutePlan(nodes.ToList(), baseEdges, [], []);
    }
}