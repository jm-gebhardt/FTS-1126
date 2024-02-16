using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using JetBrains.Annotations;

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
    [PublicAPI]
    public static Vector3 AsVector3(this GraphNode node) => new(node.X, node.Y, node.Z);

    /// <summary>
    /// Converts a <see cref="GraphNode"/> to a <see cref="Vector2"/> by projecting it onto the plane.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static Vector2 AsVector2(this GraphNode node) => new(node.X, node.Y);

    /// <summary>
    /// True if the given node is a station.
    /// </summary>
    public static bool IsStation(this GraphNode node) => new[]
    {
        AgvGraph.NodeType.Charge, AgvGraph.NodeType.Drop, AgvGraph.NodeType.Pick, AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park
    }.Contains(node.Type);

    /// <summary>
    /// True if the given node is a pick station.
    /// </summary>
    public static bool IsPickStation(this GraphNode node) => new[]
    {
         AgvGraph.NodeType.Pick, AgvGraph.NodeType.PickDrop
    }.Contains(node.Type);

    /// <summary>
    /// True if the given node is a drop station.
    /// </summary>
    public static bool IsDropStation(this GraphNode node) => new[]
    {
        AgvGraph.NodeType.Drop, AgvGraph.NodeType.PickDrop
    }.Contains(node.Type);
}