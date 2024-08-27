using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Provides a command to queue a pick drop action group for an agv.
/// </summary>
/// <param name="CorrelationId">The host order correlation id.</param>
/// <param name="AssignedAgv">The assigned agv.</param>
/// <param name="PickNode">The pick node.</param>
/// <param name="HasLowBatteryLevel">The agv has a low battery level.</param>
public record QueuePickDropActionGroupForAgv(Guid CorrelationId, Guid AssignedAgv, string PickNode, bool HasLowBatteryLevel) : CorrelatedBy<Guid>;