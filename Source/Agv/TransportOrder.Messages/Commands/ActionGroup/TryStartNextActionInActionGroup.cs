namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to start the next <see cref="Action"/>
/// inside the current <see cref="ActionGroup"/> for an agv.
/// </summary>
/// <param name="AgvCorrelationId">The correlationId of the agv.</param>
public record TryStartNextActionInActionGroup(Guid AgvCorrelationId);