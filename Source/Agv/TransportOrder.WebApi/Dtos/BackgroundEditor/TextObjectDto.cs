using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// A text label placed on the canvas.
/// <c>Width</c> and <c>Height</c> are typically 0 and resolved by the renderer.
/// </summary>
[PublicAPI]
public record TextObjectDto : BackgroundObjectDto
{
    /// <inheritdoc />
    public override ObjectTypeDto ObjectType => ObjectTypeDto.Text;

    /// <summary>Text colour encoded as a 24-bit RGB integer. Null when no colour is applied.</summary>
    public int? Color { get; init; }

    /// <summary>The string content to render.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Font size in points.</summary>
    public int FontSize { get; init; }

    /// <summary>Font family name (e.g. "roboto", "monospace").</summary>
    public string FontFamily { get; init; } = string.Empty;
}
