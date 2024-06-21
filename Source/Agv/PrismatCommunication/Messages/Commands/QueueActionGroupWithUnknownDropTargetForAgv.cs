using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Represents a command to queue an action group with an unknown drop target for an agv.
/// </summary>
/// <param name="CorrelationId">The order id.</param>
/// <param name="Agv">The agv.</param>
public record QueueActionGroupWithUnknownDropTargetForAgv(Guid CorrelationId, Guid Agv) : CorrelatedBy<Guid>;