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
        AgvGraph.NodeType.Charge, AgvGraph.NodeType.Drop, AgvGraph.NodeType.Pick, AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park, AgvGraph.NodeType.Maintenance
    }.Contains(nodeType);

    /// <summary>
    /// True if node type is Charge, Drop, Pick, PickDrop or Park.
    /// </summary>
    /// <param name="node">The node with the node type to evaluate.</param>
    /// <returns>True if node type represents a station.</returns>
    public static bool IsStation(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType type)
        {
            return type.IsStation();
        }
        return ((ICollection<AgvGraph.NodeType>)node.Type).Any(n => n.IsStation());
    }

    /// <summary>
    /// True if the given node is a pick station.
    /// </summary>
    public static bool IsPickStation(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType type)
        {
            return new[]
            {
                AgvGraph.NodeType.Pick, AgvGraph.NodeType.PickDrop
            }.Contains(type);
        }
        return ((ICollection<AgvGraph.NodeType>)node.Type).Any(n => new[]
        {
            AgvGraph.NodeType.Pick, AgvGraph.NodeType.PickDrop
        }.Contains(n));
    }

    /// <summary>
    /// True if the given node is a drop station.
    /// </summary>
    public static bool IsDropStation(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType type)
        {
            return new[]
            {
                AgvGraph.NodeType.Drop, AgvGraph.NodeType.PickDrop
            }.Contains(type);
        }
        return ((ICollection<AgvGraph.NodeType>)node.Type).Any(n => new[]
        {
            AgvGraph.NodeType.Drop, AgvGraph.NodeType.PickDrop
        }.Contains(n));
    }

    /// <summary>
    /// True if a nodes needs to be reserved.
    /// </summary>
    /// <param name="node">The node.</param>
    public static bool NeedsReservation(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType type)
        {
            return new[]
            {
                AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park
            }.Contains(type);
        }
        return ((ICollection<AgvGraph.NodeType>)node.Type).Any(n => new[]
        {
            AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park
        }.Contains(n));
    }

    /// <summary>
    /// Get the types of the node that need reservation.
    /// </summary>
    /// <param name="node">The node.</param>
    public static ICollection<AgvGraph.NodeType> GetReservableNodeTypes(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType type)
        {
            return node.NeedsReservation()? [type]:[];
        }

        return ((ICollection<AgvGraph.NodeType>)node.Type).Intersect(new List<AgvGraph.NodeType>()
        {
            AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park
        }).ToList();
    }

    /// <summary>
    /// True if a nodes is a charge node.
    /// </summary>
    /// <param name="node">The node.</param>
    public static bool IsCharger(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType type)
        {
            return type == AgvGraph.NodeType.Charge;
        }
        return ((ICollection<AgvGraph.NodeType>)node.Type).Any(n => n == AgvGraph.NodeType.Charge);
    }

    /// <summary>
    /// True if a nodes is a park node.
    /// </summary>
    /// <param name="node">The node.</param>
    public static bool IsParker(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType type)
        {
            return type == AgvGraph.NodeType.Park;
        }
        return ((ICollection<AgvGraph.NodeType>)node.Type).Any(n => n == AgvGraph.NodeType.Park);
    }

    /// <summary>
    /// Get the primary type of the node.
    /// </summary>
    /// <returns></returns>
    public static AgvGraph.NodeType GetPrimaryType(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType type)
        {
            return type;
        }
        return ((ICollection<AgvGraph.NodeType>)node.Type).First();
    }

    /// <summary>
    /// Get the additional types of the node.
    /// </summary>
    /// <returns></returns>
    public static ICollection<AgvGraph.NodeType> GetAdditionalTypes(this GraphNode node)
    {
        if (node.Type is AgvGraph.NodeType)
        {
            return [];
        }
        return ((ICollection<AgvGraph.NodeType>)node.Type).Skip(1).ToList();
    }
}