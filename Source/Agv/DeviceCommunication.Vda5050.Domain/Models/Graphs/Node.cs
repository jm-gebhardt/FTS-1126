using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// The node an agv can access.
/// </summary>
[PublicAPI]
public record Node
{
    /// <summary>
    /// The id of the node.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique id of the node for this fleet.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// The position of the node.
    /// </summary>
    public NodePosition Position { get; set; } = null!;

    /// <summary>
    /// The id of the map.
    /// </summary>
    public string? MapId { get; set; }

    /// <summary>
    /// The Theta coordinate of the node.
    /// </summary>
    public float Theta { get; init; }

    /// <summary>
    /// The actions agvs can run on this node.
    /// </summary>
    public ICollection<AgvNodeAction> AgvActions { get; set; } = [];
}