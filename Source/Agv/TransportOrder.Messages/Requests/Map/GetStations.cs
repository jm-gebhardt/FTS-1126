using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Provides a request to get all stations of a map.
/// </summary>
/// <param name="CorrelationId">The map correlation id.</param>
[UsedImplicitly]
public record GetStations(Guid CorrelationId);
