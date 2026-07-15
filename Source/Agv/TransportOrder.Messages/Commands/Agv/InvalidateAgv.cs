using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to transition to set the agv into invalid state.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="SerialNumber">The serial number of the agv.</param>
public record InvalidateAgv(Guid CorrelationId, string SerialNumber) : CorrelatedBy<Guid>;