using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to handle the abortion of an action at the agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record HandleActionAbortion(Guid CorrelationId) : CorrelatedBy<Guid>;