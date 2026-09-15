using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Graph;

/// <summary>
/// Published after graph data of the AGV types has been persisted.
/// </summary>
public record AgvTypeGraphsChanged(Guid CorrelationId) : CorrelatedBy<Guid>;