using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="GraphNode"/>
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// Calculates distance between the two nodes in 2D
    /// </summary>
    /// <param name="nodeA"></param>
    /// <param name="nodeB"></param>
    /// <returns></returns>
    public static double DistanceTo(this GraphNode nodeA, GraphNode nodeB)
    {
        var nodeAVector = new Vector2(nodeA.X, nodeA.Y);
        var nodeBVector = new Vector2(nodeB.X, nodeB.Y);
        float distance = Vector2.Distance(nodeAVector, nodeBVector);
        return distance;
    }
}