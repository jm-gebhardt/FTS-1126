namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a charging <see cref="ActionGroup"/> for an agv.
/// </summary>
/// <param name="AgvCorrelationId">The correlationId of the agv.</param>
public record CreateChargingActionGroup(Guid AgvCorrelationId);