using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;

[PublicAPI]
public static class RoutePlanGenerator
{
    public static RoutePlan Build(AgvAction action)
    {
        List<GraphNode> nodes = action.Nodes.ToList();
        List<GraphEdge> edges = Enumerable.Range(0, nodes.Count - 1)
            .Select(i => new GraphEdge(nodes[i].Id, nodes[i + 1].Id, 1f, action.Edges.ElementAt(i)))
            .ToList();
        return new RoutePlan(nodes, edges, [], []);
    }
}