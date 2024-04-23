using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the UnlockAgv request
/// </summary>
public record UnlockAgv(Guid CorrelationId) : CorrelatedBy<Guid>;