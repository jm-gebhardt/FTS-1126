using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Provides a request to get all stations of a map. A station is a node which is not a waypoint.
/// </summary>
/// <param name="CorrelationId">The map correlation id.</param>
[UsedImplicitly]
public record GetStations(Guid CorrelationId);
