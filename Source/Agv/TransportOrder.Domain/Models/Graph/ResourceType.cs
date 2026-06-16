using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// The type of graph resource a bounding box belongs to.
/// </summary>
[PublicAPI]
public enum ResourceType
{
    /// <summary>
    /// Graph node.
    /// </summary>
    Node = 0,

    /// <summary>
    /// Graph edge.
    /// </summary>
    Edge = 1
}