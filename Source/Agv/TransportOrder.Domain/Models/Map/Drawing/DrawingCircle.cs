namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;

/// <summary>
/// A circle drawing element.
/// </summary>
public record DrawingCircle : DrawingElement
{
    /// <summary>
    /// Radius of the circle in meters.
    /// </summary>
    public double Radius { get; init; }

    /// <summary>
    /// Fill color in hex format (e.g., "#FF0000") or null for no fill.
    /// </summary>
    public string? FillColor { get; init; }

    /// <summary>
    /// Width of the stroke line in pixels.
    /// </summary>
    public double LineWidth { get; init; }
}
