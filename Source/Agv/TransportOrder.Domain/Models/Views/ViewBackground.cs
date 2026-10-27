using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Views;

/// <summary>
/// The placement of a view's background image.
/// </summary>
/// <param name="Resolution">The resolution of the image in [m] per pixel.</param>
/// <param name="XOffset">The x offset in [m] where the graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset in [m] where the graph coordinate system starts. Origin is the left upper corner.</param>
[PublicAPI]
public record ViewBackground(double Resolution, double XOffset, double YOffset);