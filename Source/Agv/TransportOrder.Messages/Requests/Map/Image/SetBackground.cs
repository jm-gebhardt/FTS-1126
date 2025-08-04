using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;

/// <summary>
/// Command to store (or overwrite) a custom background image for a map.
/// </summary>
/// <param name="CorrelationId">Correlation‑Id of the map.</param>
/// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented</param>
/// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="Image">PNG image bytes.</param>
public record SetBackground(Guid CorrelationId, double Resolution, double XOffset, double YOffset, byte[] Image) : CorrelatedBy<Guid>;