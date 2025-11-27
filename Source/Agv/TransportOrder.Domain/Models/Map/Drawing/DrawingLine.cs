namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;

/// <summary>
/// A line (polyline) drawing element.
/// </summary>
public record DrawingLine : DrawingElement
{
    /// <summary>
    /// The points that make up the line path, in world coordinates (meters).
    /// </summary>
    public IReadOnlyList<Position2D> Points { get; init; } = [];

    /// <summary>
    /// Width of the line in pixels.
    /// </summary>
    public double LineWidth { get; init; }
}
