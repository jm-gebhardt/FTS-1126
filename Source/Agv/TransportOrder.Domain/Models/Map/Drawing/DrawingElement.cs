using System.Text.Json.Serialization;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;

/// <summary>
/// Base class for all drawing elements.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DrawingRectangle), "rectangle")]
[JsonDerivedType(typeof(DrawingCircle), "circle")]
[JsonDerivedType(typeof(DrawingLine), "line")]
[JsonDerivedType(typeof(DrawingText), "text")]
public abstract record DrawingElement
{
    /// <summary>
    /// Unique identifier of the element.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The position of the element in world coordinates (meters).
    /// For rectangles and circles, this is the center position.
    /// For lines, this is the first point.
    /// For text, this is the anchor position.
    /// </summary>
    public required Position2D Position { get; init; }

    /// <summary>
    /// Stroke color in hex format (e.g., "#FF0000") or null for no stroke.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Z-index for layering order. Higher values are rendered on top.
    /// </summary>
    public int ZIndex { get; init; }
}
