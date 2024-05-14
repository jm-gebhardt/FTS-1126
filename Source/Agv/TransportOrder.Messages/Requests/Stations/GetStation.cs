using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Provides a request to get a specific station. A station is a node which is not a waypoint.
/// </summary>
/// <param name="CorrelationId">The correlation id of the station.</param>
[UsedImplicitly]
public record GetStation(string CorrelationId) : CorrelatedBy<string>;