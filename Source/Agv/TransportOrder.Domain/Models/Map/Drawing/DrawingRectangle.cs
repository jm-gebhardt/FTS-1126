namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;

/// <summary>
/// A rectangle drawing element.
/// </summary>
public record DrawingRectangle : DrawingElement
{
    /// <summary>
    /// Width of the rectangle in meters.
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// Height of the rectangle in meters.
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// Fill color in hex format (e.g., "#FF0000") or null for no fill.
    /// </summary>
    public string? FillColor { get; init; }

    /// <summary>
    /// Width of the stroke line in pixels.
    /// </summary>
    public double LineWidth { get; init; }

    /// <summary>
    /// Rotation angle in radians.
    /// </summary>
    public double Rotation { get; init; }
}
