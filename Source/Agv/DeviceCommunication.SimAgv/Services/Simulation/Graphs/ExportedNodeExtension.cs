using System.Numerics;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Provides extension method for <see cref="ExportedNode"/>
/// </summary>
public static class ExportedNodeExtension
{
    /// <summary>
    /// Get the <see cref="Vector2"/> representation of a node.
    /// </summary>
    /// <param name="node">The node</param>
    /// <returns>Vector to using x, y coordinates of the node.</returns>
    public static Vector2 AsVector2(this ExportedNode node)
    {
        return new Vector2((float)node.X, (float)node.Y);
    }
}