using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Routing.Exceptions;

/// <summary>
/// When there was no route found for the given source/destination nodes
/// </summary>
public class RouteNotFoundException : Exception
{
    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="destinationNodeId"></param>
    public RouteNotFoundException(string? sourceNodeId, string destinationNodeId) : base($"No route found {sourceNodeId}->{destinationNodeId}")
    {
    }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="destinationNodeIds"></param>
    public RouteNotFoundException(string? sourceNodeId, IEnumerable<string> destinationNodeIds) : base($"No route found {sourceNodeId}{string.Join("-> ", destinationNodeIds)}")
    {
    }

    /// <summary>
    /// If no route was found for the given agv to any of these nodes
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="destinationNodeIds"></param>
    public RouteNotFoundException(IAgv agv, IList<GraphNode> destinationNodeIds) : base($"No route found to any of these nodes: {agv}{string.Join(", ", destinationNodeIds.Select(n => n.Id))}")
    {
    }
}