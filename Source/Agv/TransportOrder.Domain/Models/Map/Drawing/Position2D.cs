namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;

/// <summary>
/// Represents a 2D position in world coordinates (meters).
/// </summary>
public record Position2D
{
    /// <summary>
    /// The X coordinate in meters.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// The Y coordinate in meters.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Creates a new 2D position.
    /// </summary>
    public Position2D() { }

    /// <summary>
    /// Creates a new 2D position with the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate in meters.</param>
    /// <param name="y">The Y coordinate in meters.</param>
    public Position2D(double x, double y)
    {
        X = x;
        Y = y;
    }
}
