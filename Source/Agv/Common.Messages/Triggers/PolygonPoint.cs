namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// A polygon point.
/// </summary>
public record PolygonPoint
{
    /// <summary>
    /// X-position [m]
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Y-position [m]
    /// </summary>
    public double Y { get; }

    /// <inheritdoc cref="PolygonPoint"/>
    /// <param name="x">X-position [m]</param>
    /// <param name="y">Y-position [m]</param>
    public PolygonPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <inheritdoc cref="PolygonPoint"/>>
    public PolygonPoint()
    {
    }
}