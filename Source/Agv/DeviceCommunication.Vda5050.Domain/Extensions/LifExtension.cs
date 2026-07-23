using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Provides extension methods for lif objects.
/// </summary>
public static class LifExtension
{
    extension(List<Node> nodes)
    {
        /// <summary>
        /// Resolve the node that is defined for this agv and node id.
        /// </summary>
        /// <param name="nodeId">The node id</param>
        /// <param name="agvCorrelationId">The agv correlation id.</param>
        /// <exception cref="NodeNotDefinedForAgvException">If node can not be found</exception>
        public Node ResolveNode(Guid nodeId, Guid agvCorrelationId)
        {
            // Use fleetmanager id instead of nodeId.ToGuid, once available
            return nodes.SingleOrDefault(n => n.NodeId.ToGuid() == nodeId) ?? throw new NodeNotDefinedForAgvException(nodeId, agvCorrelationId);
        }
    }

    extension(List<Edge> edges)
    {
        /// <summary>
        /// Resolve the edge that is defined for this agv and edge id.
        /// </summary>
        /// <param name="edgeId">The edge id</param>
        /// <param name="agvCorrelationId">The agv correlation id.</param>
        /// <exception cref="NodeNotDefinedForAgvException">If node can not be found</exception>
        public Edge ResolveEdge(Guid edgeId, Guid agvCorrelationId)
        {
            // Use fleetmanager id instead of edgeId.ToGuid, once available
            return edges.SingleOrDefault(e => e.EdgeId.ToGuid() == edgeId) ?? throw new EdgeNotDefinedForAgvException(edgeId, agvCorrelationId);
        }
    }
}
