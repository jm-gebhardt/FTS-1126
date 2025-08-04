using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// Form data for uploading or replacing a background image for a map.
/// </summary>
/// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented</param>
/// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
[UsedImplicitly]
public record UpdateMapDto(double Resolution, double XOffset, double YOffset);