using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to handle failure of an action.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group</param>
/// <param name="ActionCorrelationId">The correlation id of the failed action.</param>
public record HandleActionFailure(Guid CorrelationId, Guid ActionCorrelationId) : CorrelatedBy<Guid>;