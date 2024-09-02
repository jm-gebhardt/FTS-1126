using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filters;

/// <summary>
/// Node filters for Entity Framework.
/// </summary>
[PublicAPI]
public static class NodeFilters
{
    /// <summary>
    /// Filter for nodes that are stations.
    /// </summary>
    public static readonly Expression<Func<EfNode, bool>> IsStation = node =>
        node.Type == AgvGraph.NodeType.Charge ||
        node.Type == AgvGraph.NodeType.Drop ||
        node.Type == AgvGraph.NodeType.Pick ||
        node.Type == AgvGraph.NodeType.PickDrop ||
        node.Type == AgvGraph.NodeType.Park;
}