using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to send an agv to the closest charger.
/// </summary>
public record SendAgvToCharger(Guid CorrelationId) : CorrelatedBy<Guid>;