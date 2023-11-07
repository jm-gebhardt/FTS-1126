namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to abort an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record AbortActionGroup(Guid CorrelationId);