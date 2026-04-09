using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

/// <summary>
/// A triangular canvas object with an optional border.
/// </summary>
[PublicAPI]
public class TriangleObject : BackgroundObject
{
    /// <summary>Fill colour as a 24-bit RGB integer. Null when no fill is applied.</summary>
    public int? Color { get; set; }

    /// <summary>Border stroke width in pixels. 0 means no border.</summary>
    public int BorderWidth { get; set; }

    /// <summary>Border colour as a 24-bit RGB integer. Null when no border is drawn.</summary>
    public int? BorderColor { get; set; }

    /// <inheritdoc cref="TriangleObject"/>
    public TriangleObject(Guid backgroundId, int externalId, double x, double y,
        double width, double height, double rotation, double scaleX, double scaleY,
        int? color, int borderWidth, int? borderColor)
        : base(backgroundId, externalId, x, y, width, height, rotation, scaleX, scaleY)
    {
        Color = color;
        BorderWidth = borderWidth;
        BorderColor = borderColor;
    }

    /// <summary>Parameterless constructor for EF Core.</summary>
    [UsedImplicitly]
    private TriangleObject() { }
}
