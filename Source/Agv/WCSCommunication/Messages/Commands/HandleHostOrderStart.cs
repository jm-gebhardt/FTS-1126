using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Commands;

/// <summary>
/// Provides a command to handle the start of a host order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="AssignedAgv">The assigned agv</param>
/// <param name="Timestamp">The time the order was started.</param>
public record HandleHostOrderStart(Guid CorrelationId, string AssignedAgv, DateTime Timestamp) : CorrelatedBy<Guid>;