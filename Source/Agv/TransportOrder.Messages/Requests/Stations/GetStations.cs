using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Provides a request to get all stations. A station is a node which is not a waypoint.
/// </summary>
/// <param name="CorrelationId">The correlation id of the request.</param>
[UsedImplicitly]
public record GetStations(Guid CorrelationId) : CorrelatedBy<Guid>;