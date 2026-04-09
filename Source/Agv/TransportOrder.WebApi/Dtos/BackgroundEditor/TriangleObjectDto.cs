using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// A triangular canvas object with an optional border.
/// </summary>
[PublicAPI]
public record TriangleObjectDto : BackgroundObjectDto
{
    /// <inheritdoc />
    public override ObjectTypeDto ObjectType => ObjectTypeDto.Triangle;

    /// <summary>Fill colour encoded as a 24-bit RGB integer. Null when no fill is applied.</summary>
    public int? Color { get; init; }

    /// <summary>Width of the border stroke in pixels. 0 means no border.</summary>
    public int BorderWidth { get; init; }

    /// <summary>Border colour encoded as a 24-bit RGB integer. Null when no border is drawn.</summary>
    public int? BorderColor { get; init; }
}
