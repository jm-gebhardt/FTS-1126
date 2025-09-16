namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to charge an agv at a given charger.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="ChargerId">The node id of the charger.</param>
/// <param name="User">The user who charged the agv.</param>
public record ChargeAgv(Guid CorrelationId, Guid ChargerId, string User);