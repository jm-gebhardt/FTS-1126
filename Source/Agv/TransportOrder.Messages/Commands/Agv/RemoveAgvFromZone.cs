using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Removes an agv from the provided zone.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="ZoneId">The zone id.</param>
/// <param name="User">The user who removed the agv from a zone.</param>
[UsedImplicitly]
public record RemoveAgvFromZone(Guid CorrelationId, string ZoneId, string User) : CorrelatedBy<Guid>;