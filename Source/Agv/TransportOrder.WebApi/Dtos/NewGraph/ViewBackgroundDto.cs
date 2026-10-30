using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// The placement of a view's background image.
/// </summary>
/// <param name="XOffset">The x offset in [m] where the graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset in [m] where the graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="Resolution">The resolution of the image in [m] per pixel.</param>
[PublicAPI]
public record ViewBackgroundDto(double XOffset, double YOffset, double Resolution);