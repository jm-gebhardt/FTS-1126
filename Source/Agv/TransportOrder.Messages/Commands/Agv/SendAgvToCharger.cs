using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to send an agv to the closest charger.
/// </summary>
/// <param name="CorrelationId">The correlation id of the AGV.</param>
/// <param name="User">The user who charged the agv.</param>
public record SendAgvToCharger(Guid CorrelationId, string User) : CorrelatedBy<Guid>;