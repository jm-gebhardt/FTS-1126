using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

/// <summary>
/// Abstract base for all drawable objects on a background canvas.
/// Stored using Table-Per-Hierarchy; concrete subclasses add their own columns.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(RectangleObject), "rectangle")]
[JsonDerivedType(typeof(TriangleObject), "triangle")]
[JsonDerivedType(typeof(CircleObject), "circle")]
[JsonDerivedType(typeof(LineObject), "line")]
[JsonDerivedType(typeof(PolylineObject), "polyline")]
[JsonDerivedType(typeof(TextObject), "text")]
[PublicAPI]
public abstract class BackgroundObject
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The numeric id from the source JSON document.
    /// Preserved so the document can be round-tripped without re-numbering.
    /// </summary>
    public int ExternalId { get; set; }

    /// <summary>Foreign key to the owning <see cref="Background"/>.</summary>
    public Guid BackgroundId { get; set; }

    /// <summary>Horizontal position on the canvas (world units).</summary>
    public double X { get; set; }

    /// <summary>Vertical position on the canvas (world units).</summary>
    public double Y { get; set; }

    /// <summary>Bounding-box width before scaling.</summary>
    public double Width { get; set; }

    /// <summary>Bounding-box height before scaling.</summary>
    public double Height { get; set; }

    /// <summary>Rotation angle in radians.</summary>
    public double Rotation { get; set; }

    /// <summary>Horizontal scale factor.</summary>
    public double ScaleX { get; set; }

    /// <summary>Vertical scale factor.</summary>
    public double ScaleY { get; set; }

    /// <inheritdoc cref="BackgroundObject"/>
    protected BackgroundObject(Guid backgroundId, int externalId, double x, double y,
        double width, double height, double rotation, double scaleX, double scaleY)
    {
        Id = Guid.NewGuid();
        BackgroundId = backgroundId;
        ExternalId = externalId;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Rotation = rotation;
        ScaleX = scaleX;
        ScaleY = scaleY;
    }

    /// <summary>Parameterless constructor for EF Core.</summary>
    [UsedImplicitly]
    protected BackgroundObject()
    {
        Id = Guid.NewGuid();
    }
}
