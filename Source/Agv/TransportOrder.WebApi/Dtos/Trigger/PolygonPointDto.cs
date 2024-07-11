using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

/// <summary>
/// A dto for polygon points
/// </summary>
[PublicAPI]
public record PolygonPointDto
{
    /// <summary>
    /// The x coordinate of the point.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// The y coordinate of the point.
    /// </summary>
    public double Y { get; set; }

    /// <inheritdoc cref="PolygonPointDto"/>>
    public PolygonPointDto(double x, double y)
    {
        X = x;
        Y = y;
    }
}