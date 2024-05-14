using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Extensions;

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
    /// True if node type is Charge, Drop, Pick, PickDrop or Park.
    /// </summary>
    /// <param name="nodeType">The node type to evaluate.</param>
    /// <returns>True if node type represents a station.</returns>
    private static bool IsStation(this AgvGraph.NodeType nodeType) => new[]
    {
        AgvGraph.NodeType.Charge, AgvGraph.NodeType.Drop, AgvGraph.NodeType.Pick, AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park
    }.Contains(nodeType);

    /// <summary>
    /// True if node type is Charge, Drop, Pick, PickDrop or Park.
    /// </summary>
    /// <param name="node">The node with the node type to evaluate.</param>
    /// <returns>True if node type represents a station.</returns>
    public static bool IsStation(this GraphNode node)
    {
        return node.Type.IsStation();
    }

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

    /// <summary>
    /// True if a nodes needs to be reserved.
    /// </summary>
    /// <param name="node">The node.</param>
    public static bool NeedsReservation(this GraphNode node) => new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}.Contains(node.Type);

    /// <summary>
    /// True if a nodes is a charge node.
    /// </summary>
    /// <param name="node">The node.</param>
    public static bool IsCharger(this GraphNode node) => node.Type == AgvGraph.NodeType.Charge;
}