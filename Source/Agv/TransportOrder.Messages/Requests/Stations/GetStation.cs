using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Provides a request to get a specific station.
/// </summary>
/// <param name="CorrelationId">The correlation id of the station.</param>
[UsedImplicitly]
public record GetStation(Guid CorrelationId) : CorrelatedBy<Guid>;