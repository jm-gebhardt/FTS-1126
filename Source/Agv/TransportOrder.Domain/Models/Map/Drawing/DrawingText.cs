namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;

/// <summary>
/// A text drawing element.
/// </summary>
public record DrawingText : DrawingElement
{
    /// <summary>
    /// The text content to display.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Font size in pixels.
    /// </summary>
    public double FontSize { get; init; }

    /// <summary>
    /// Font weight: "normal" or "bold".
    /// </summary>
    public string FontWeight { get; init; } = "normal";
}
