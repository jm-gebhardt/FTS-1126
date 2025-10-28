using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Try to retract an action group .
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record TryRetractActionGroup(Guid CorrelationId) : CorrelatedBy<Guid>;