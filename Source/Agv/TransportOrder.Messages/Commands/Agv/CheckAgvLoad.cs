using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to check if the load on the agv matches current actions.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record CheckAgvLoad(Guid CorrelationId) : CorrelatedBy<Guid>;