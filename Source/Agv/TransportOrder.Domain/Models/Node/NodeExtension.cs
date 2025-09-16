using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Node;

/// <summary>
/// Domain internal extensions for <see cref="GraphNode"/>
/// </summary>
public static class NodeExtension
{
    /// <summary>
    /// Try select a node from the collection by id
    /// </summary>
    /// <param name="nodes">The node collection.</param>
    /// <param name="nodeId">The node id.</param>
    /// <returns>The selected node.</returns>
    /// <exception cref="NodeNotFoundException">When the node is not found.</exception>
    public static GraphNode SelectById(this IEnumerable<GraphNode> nodes, Guid nodeId)
    {
        return nodes.FirstOrDefault(n => n.Id == nodeId) ?? throw new NodeNotFoundException(nodeId);
    }
}