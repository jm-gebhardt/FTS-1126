using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;

/// <summary>
/// Provides extension method for <see cref="ExportedNode"/>
/// </summary>
public static class ExportedNodeExtension
{
    /// <summary>
    /// Converts an exported node to a <see cref="GraphNode"/>.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The graph node.</returns>
    public static GraphNode ToGraphNode(this ExportedNode node)
    {
        return new GraphNode(node.NodeId, (float) node.X, -(float) node.Y, 0, AgvGraph.NodeType.WayPoint, null, null, null) { TimeStamp = node.TimeStamp };
    }
}