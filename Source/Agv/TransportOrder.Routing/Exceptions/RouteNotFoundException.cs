using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Routing.Exceptions;

/// <summary>
/// When there was no route found for the given source/destination nodes
/// </summary>
public class RouteNotFoundException : NotFoundException
{
    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="destinationNodeId"></param>
    public RouteNotFoundException(Guid? sourceNodeId, Guid destinationNodeId) : base($"No route found {sourceNodeId}->{destinationNodeId}")
    {
    }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="destinationNodeIds"></param>
    public RouteNotFoundException(Guid? sourceNodeId, IEnumerable<Guid> destinationNodeIds) : base($"No route found {sourceNodeId}{string.Join("-> ", destinationNodeIds)}")
    {
    }

    /// <summary>
    /// If no route was found for the given agv to any of these nodes.
    /// </summary>
    public RouteNotFoundException(Guid agvCorrelationId, IEnumerable<GraphNode> destinationNodeIds) : base($"No route found to any of these nodes: {agvCorrelationId} {string.Join(", ", destinationNodeIds.Select(n => n.Name))}")
    {
    }
}