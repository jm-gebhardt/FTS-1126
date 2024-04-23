using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to try and reactivate agvs that are currently charging.
/// </summary>
public record TryReactivateChargingAgv(Guid CorrelationId) : CorrelatedBy<Guid>;