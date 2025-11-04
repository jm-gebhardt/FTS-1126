using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;

/// <summary>
/// Provides a dto for a queue node.
/// </summary>
[PublicAPI]
public record QueueNodeDto
{
    /// <summary>
    /// The node id.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// The position of the node in the queue.
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// The agv the node is reserved for.
    /// </summary>
    public Guid? ReservedAgv { get; private set; }

    /// <summary>
    /// The agv that occupies the queue node.
    /// </summary>
    public Guid? OccupiedAgv { get; private set; }

    /// <inheritdoc cref="QueueNodeDto"/>>
    [JsonConstructor]
    public QueueNodeDto()
    {
    }
}