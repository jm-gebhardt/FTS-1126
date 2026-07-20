using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when the operating mode of the agv changed.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Agv">The agv.</param>
public record AgvOperatingModeChanged(Guid CorrelationId, MessageContent.Agv Agv) : CorrelatedBy<Guid>;