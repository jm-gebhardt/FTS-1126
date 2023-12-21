using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Extensions for <see cref="GraphNode"/>
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// Converts a <see cref="GraphNode"/> to a <see cref="Vector3"/>
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static Vector3 AsVector3(this GraphNode node) => new(node.X, node.Y, node.Z);
}