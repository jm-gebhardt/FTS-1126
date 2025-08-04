using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Command to overwrite the background parameters of a map.
/// </summary>
/// <param name="CorrelationId">Correlation‑Id of the map.</param>
/// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented</param>
/// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
public record UpdateMap(Guid CorrelationId, double Resolution, double XOffset, double YOffset) : CorrelatedBy<Guid>;