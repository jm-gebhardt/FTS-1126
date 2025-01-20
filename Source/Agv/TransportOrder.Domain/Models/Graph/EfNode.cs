using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// Model for a node in the AGV graph, it has X/Y/Z coordinates and a type. This model is for EF only
/// </summary>
public class EfNode
{
    /// <summary>
    /// The ID of the node, based on the AGV vendor
    /// </summary><remarks>
    /// Usually this is the (numeric) ID passed by the AGV vendor.
    /// For internal reasons it can be necessary to build/combine an internal identification.
    /// </remarks>
    public string Id { get; init; }

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

    [JsonConstructor]
    private EfNode()
    {
        Id = null!;
        Maps = null!;
        AgvGraphs = null!;
    }
}