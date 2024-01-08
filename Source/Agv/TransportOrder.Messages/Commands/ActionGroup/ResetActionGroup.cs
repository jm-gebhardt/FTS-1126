using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to reset an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record ResetActionGroup(Guid CorrelationId) : CorrelatedBy<Guid>;