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
    public string NodeId { get; init; } = string.Empty;

    /// <summary>
    /// The position of the node in the queue.
    /// </summary>
    public int Position { get; init; }

    /// <inheritdoc cref="QueueNodeDto"/>>
    public QueueNodeDto(string nodeId, int position)
    {
        NodeId = nodeId;
        Position = position;
    }

    /// <inheritdoc cref="QueueNodeDto"/>>
    [JsonConstructor]
    public QueueNodeDto()
    {
    }
}