using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Commands;

/// <summary>
/// Provides a command to the finish the cancellation of a host order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="Timestamp">The time the order was canceled.</param>
public record FinishHostOrderCancellation(Guid CorrelationId, DateTime Timestamp) : CorrelatedBy<Guid>;