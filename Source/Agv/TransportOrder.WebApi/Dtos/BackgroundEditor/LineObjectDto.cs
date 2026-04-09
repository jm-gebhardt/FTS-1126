using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// A straight line segment defined by exactly two points.
/// </summary>
[PublicAPI]
public record LineObjectDto : BackgroundObjectDto
{
    /// <inheritdoc />
    public override ObjectTypeDto ObjectType => ObjectTypeDto.Line;

    /// <summary>Stroke colour encoded as a 24-bit RGB integer. Null when no colour is applied.</summary>
    public int? Color { get; init; }

    /// <summary>Stroke width in pixels.</summary>
    public int LineWidth { get; init; }

    /// <summary>Stroke style (solid or dashed).</summary>
    public LineStyleDto LineStyle { get; init; }

    /// <summary>
    /// The two end-points of the line in object-local coordinates.
    /// Coordinates are relative to the object's <c>X</c>/<c>Y</c> origin.
    /// </summary>
    public IReadOnlyList<PointDto> Points { get; init; } = [];
}
