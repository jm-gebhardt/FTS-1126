using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when an agv left a station
/// </summary>
/// <param name="CorrelationId">The correlation id of the station</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record AgvLeftStation(Guid CorrelationId, Guid AgvCorrelationId) : CorrelatedBy<Guid>;