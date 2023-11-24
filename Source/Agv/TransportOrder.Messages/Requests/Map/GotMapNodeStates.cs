using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Provides the response to the <see cref="GetMapNodeStates"/> request.
/// </summary>
/// <param name="NodeLocks">The node states.</param>
public record GotMapNodeStates(ICollection<NodeLock> NodeLocks);