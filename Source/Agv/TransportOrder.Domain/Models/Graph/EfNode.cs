using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;

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
    /// The CorrelationId of the AgvGraph, which is also the AgvCorrelationId
    /// </summary>
    public Guid GraphCorrelationId { get; init; }

    /// <summary>
    /// The type of the node
    /// </summary>
    public AgvGraph.NodeType Type { get; init; }

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

    private EfNode()
    {
        Id = null!;
    }
}