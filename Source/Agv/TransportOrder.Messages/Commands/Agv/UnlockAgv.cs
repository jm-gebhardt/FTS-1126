using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the UnlockAgv request
/// </summary>
public record UnlockAgv(Guid CorrelationId) : CorrelatedBy<Guid>;