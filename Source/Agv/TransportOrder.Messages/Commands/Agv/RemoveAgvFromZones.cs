using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to remove the agv from given zones.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Zones">The zones the agv was removed from.</param>);
public record RemoveAgvFromZones(Guid CorrelationId, ICollection<string> Zones) : CorrelatedBy<Guid>;
