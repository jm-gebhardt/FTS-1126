using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// An open path through an ordered sequence of points.
/// </summary>
[PublicAPI]
public record PolylineObjectDto : BackgroundObjectDto
{
    /// <inheritdoc />
    public override ObjectTypeDto ObjectType => ObjectTypeDto.Polyline;

    /// <summary>Stroke colour encoded as a 24-bit RGB integer. Null when no colour is applied.</summary>
    public int? Color { get; init; }

    /// <summary>Stroke width in pixels.</summary>
    public int LineWidth { get; init; }

    /// <summary>Stroke style (solid or dashed).</summary>
    public LineStyleDto LineStyle { get; init; }

    /// <summary>
    /// Ordered list of vertices in object-local coordinates.
    /// Coordinates are relative to the object's <c>X</c>/<c>Y</c> origin.
    /// </summary>
    public IReadOnlyList<PointDto> Points { get; init; } = [];
}
