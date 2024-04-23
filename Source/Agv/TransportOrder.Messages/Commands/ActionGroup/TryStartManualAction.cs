namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to start a manual <see cref="Action"/> for an agv.
/// </summary>
/// <param name="AgvCorrelationId">The correlationId of the agv.</param>
public record TryStartManualAction(Guid AgvCorrelationId);