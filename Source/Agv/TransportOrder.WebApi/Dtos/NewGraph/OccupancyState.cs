using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// Traffic management state: Whether an AGV uses a node.
/// </summary>
[PublicAPI]
public enum OccupancyState
{
    /// <summary>
    /// No AGV uses the node.
    /// </summary>
    Free,

    /// <summary>
    /// The node is reserved for an AGV.
    /// </summary>
    Reserved,

    /// <summary>
    /// An AGV occupies the node.
    /// </summary>
    Occupied
}