namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to complete an action.
/// </summary>
/// <param name="ActionCorrelationId">The correlation id o the action.</param>
public record CompleteAction(Guid ActionCorrelationId);