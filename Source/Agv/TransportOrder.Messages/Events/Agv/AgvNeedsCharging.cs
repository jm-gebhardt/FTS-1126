using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Event sent when an AGV needs to be moved to a charging station, because it's Soc is decreased (below low)
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// </summary>
public record AgvNeedsCharging(Guid CorrelationId) : CorrelatedBy<Guid>;