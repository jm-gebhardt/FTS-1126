using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to abort the current order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="ActionId">The id of the cancelOrder action.</param>
public record AbortOrder(Guid CorrelationId, string ActionId) : CorrelatedBy<Guid>;