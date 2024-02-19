namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to charge an agv at a given charger.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="ChargerId">The node id of the charger.</param>
public record ChargeAgv(Guid CorrelationId, string ChargerId);