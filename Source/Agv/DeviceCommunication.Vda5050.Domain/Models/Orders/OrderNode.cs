using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Node to be traversed for fulfilling an order.
/// </summary>
[PublicAPI]
public class OrderNode
{
    /// <summary>
    /// Unique node identification.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Number to track the sequence of nodes and edges in an order and to simplify order updates.
    /// Runs across all nodes and edges of the same order and is reset when a new orderId is issued.
    /// </summary>
    public int SequenceId { get; set; }

    /// <summary>
    /// Additional information on the node.
    /// </summary>
    public string? NodeDescription { get; set; }

    /// <summary>
    /// True indicates that the node is part of the base.
    /// False indicates that the node is part of the horizon.
    /// </summary>
    public bool Released { get; set; }

    /// <summary>
    /// Position of the node on a map in world coordinates.
    /// Optional for vehicle types that do not require the node position (e.g., line-guided vehicles).
    /// </summary>
    public OrderNodePosition? NodePosition { get; set; }

    /// <summary>
    /// Array of actions to be executed on this node. Empty array if no actions required.
    /// </summary>
    public ICollection<OrderAction> Actions { get; set; } = [];

    /// <summary>
    /// Release the node.
    /// </summary>
    public void Release()
    {
        Released = true;
    }
}