using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the last node id of an agv has changed.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="OldLastNodeId">The last node id before the change.</param>
/// <param name="NewLastNodeId">The last node id after the change.</param>
public record AgvLastNodeIdChanged(Guid CorrelationId, string? OldLastNodeId, string? NewLastNodeId) : CorrelatedBy<Guid>;