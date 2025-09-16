using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// Model for a node in the AGV graph, it has X/Y/Z coordinates and a type. This model is for EF only
/// </summary>
public class EfNode
{
    /// <summary>
    /// The unique Id of the node.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The Name of the node.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The time (UTC) when this node was created.
    /// </summary>
    public DateTime TimeStamp
    {
        get;
        [UsedImplicitly]
        set;
    }

    /// <summary>
    /// The type of the node
    /// </summary>
    [JsonConverter(typeof(JsonNodeTypeConverter))]
    public object Type { get; init; } = null!;

    /// <summary>
    /// The X coordinate of the node
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// The Y coordinate of the node
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// The Z coordinate of the node
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// The Theta coordinate of the node
    /// </summary>
    public float Theta { get; init; }

    /// <summary>
    /// The Maps the node is part of.
    /// </summary>
    public ICollection<Map.Map> Maps { get; init; }

    /// <summary>
    /// The AgvGraphs the edge is part of.
    /// </summary>
    public ICollection<EfAgvGraph> AgvGraphs { get; init; }

    /// <summary>
    /// The drop lift height [mm], if set.
    /// </summary>
    public double? DropLiftHeight { get; init; }

    /// <summary>
    /// The pick lift height [mm], if set.
    /// </summary>
    public double? PickLiftHeight { get; init; }

    /// <summary>
    /// The docking direction of the node, if set.
    /// </summary>
    public DockingDirection? DockingDirection { get; init; }

    /// <summary>
    /// EF
    /// </summary>
    [JsonConstructor]
    public EfNode()
    {
        Name = null!;
        Maps = [];
        AgvGraphs = null!;
    }
}