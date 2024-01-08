using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to try and reactivate agvs that are currently charging.
/// </summary>
public record TryReactivateChargingAgv(Guid CorrelationId) : CorrelatedBy<Guid>;