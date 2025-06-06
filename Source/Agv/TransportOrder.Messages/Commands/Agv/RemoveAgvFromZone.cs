using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Removes an agv from the provided zone.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="ZoneId">The zone id.</param>
[UsedImplicitly]
public record RemoveAgvFromZone(Guid CorrelationId, string ZoneId) : CorrelatedBy<Guid>;