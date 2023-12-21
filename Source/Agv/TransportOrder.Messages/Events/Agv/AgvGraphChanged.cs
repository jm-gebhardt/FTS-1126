using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Event triggered every time the AgvGraph has changed / was received.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record AgvGraphChanged(Guid CorrelationId) : CorrelatedBy<Guid>;