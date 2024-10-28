using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;

/// <summary>
/// Provides a dto for a queue.
/// </summary>
[PublicAPI]
public record QueueDto : DatedMessage
{
    /// <summary>
    /// The id of the queue.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The name of the queue.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The nodes in the queue.
    /// </summary>
    public ICollection<QueueNodeDto> Nodes { get; init; } = [];
}