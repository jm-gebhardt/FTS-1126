using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// A circular (or elliptical, when scaled) canvas object with an optional border.
/// <c>Width</c> and <c>Height</c> represent the diameter before <c>ScaleX</c>/<c>ScaleY</c> are applied.
/// </summary>
[PublicAPI]
public record CircleObjectDto : BackgroundObjectDto
{
    /// <inheritdoc />
    public override ObjectTypeDto ObjectType => ObjectTypeDto.Circle;

    /// <summary>Fill colour encoded as a 24-bit RGB integer. Null when no fill is applied.</summary>
    public int? Color { get; init; }

    /// <summary>Width of the border stroke in pixels. 0 means no border.</summary>
    public int BorderWidth { get; init; }

    /// <summary>Border colour encoded as a 24-bit RGB integer. Null when no border is drawn.</summary>
    public int? BorderColor { get; init; }
}
