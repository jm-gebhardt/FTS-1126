using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

/// <summary>
/// A text label placed on the canvas.
/// </summary>
[PublicAPI]
public class TextObject : BackgroundObject
{
    /// <summary>Text colour as a 24-bit RGB integer. Null when no colour is applied.</summary>
    public int? Color { get; set; }

    /// <summary>The string content to render.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Font size in points.</summary>
    public int FontSize { get; set; }

    /// <summary>Font family name (e.g. "roboto", "monospace").</summary>
    public string FontFamily { get; set; } = string.Empty;

    /// <inheritdoc cref="TextObject"/>
    public TextObject(Guid backgroundId, int externalId, double x, double y,
        double width, double height, double rotation, double scaleX, double scaleY,
        int? color, string text, int fontSize, string fontFamily)
        : base(backgroundId, externalId, x, y, width, height, rotation, scaleX, scaleY)
    {
        Color = color;
        Text = text;
        FontSize = fontSize;
        FontFamily = fontFamily;
    }

    /// <summary>Parameterless constructor for EF Core.</summary>
    [UsedImplicitly]
    private TextObject() { }
}
