using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

/// <summary>
/// A straight line segment defined by exactly two <see cref="BackgroundObjectPoint"/> vertices.
/// </summary>
[PublicAPI]
public class LineObject : BackgroundObject
{
    /// <summary>Stroke colour as a 24-bit RGB integer. Null when no colour is applied.</summary>
    public int? Color { get; set; }

    /// <summary>Stroke width in pixels.</summary>
    public int LineWidth { get; set; }

    /// <summary>Stroke style (solid or dashed).</summary>
    public LineStyle LineStyle { get; set; }

    /// <summary>The two end-points in object-local coordinates, ordered by <see cref="BackgroundObjectPoint.Index"/>.</summary>
    public ICollection<BackgroundObjectPoint> Points { get; init; } = [];

    /// <inheritdoc cref="LineObject"/>
    public LineObject(Guid backgroundId, int externalId, double x, double y,
        double width, double height, double rotation, double scaleX, double scaleY,
        int? color, int lineWidth, LineStyle lineStyle)
        : base(backgroundId, externalId, x, y, width, height, rotation, scaleX, scaleY)
    {
        Color = color;
        LineWidth = lineWidth;
        LineStyle = lineStyle;
    }

    /// <summary>Parameterless constructor for EF Core.</summary>
    [UsedImplicitly]
    private LineObject() { }
}
