using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Provides the response to the <see cref="GetMapNodeStates"/> request.
/// </summary>
/// <param name="NodeStates">The node states.</param>
public record GotMapNodeStates(ICollection<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser)> NodeStates);