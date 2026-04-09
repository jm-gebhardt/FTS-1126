using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// The set of predefined colours available in the background editor.
/// Each colour is encoded as a 24-bit RGB integer.
/// </summary>
/// <param name="LineColor">Palette of colours available for strokes and borders.</param>
/// <param name="FillColor">Palette of colours available for fills.</param>
[PublicAPI]
public record ColorPaletteDto(
    IReadOnlyList<int> LineColor,
    IReadOnlyList<int> FillColor);
