namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

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
    // ReSharper disable once UnusedParameter.Local
    public RouteNotFoundException(string? sourceNodeId, string destinationNodeId) : base($"No route found {sourceNodeId}->{destinationNodeId}")
    {
    }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="destinationNodeIds"></param>
    // ReSharper disable once UnusedParameter.Local
    public RouteNotFoundException(string? sourceNodeId, IEnumerable<string> destinationNodeIds) : base($"No route found {sourceNodeId}{string.Join("-> ", destinationNodeIds)}")
    {
    }
}