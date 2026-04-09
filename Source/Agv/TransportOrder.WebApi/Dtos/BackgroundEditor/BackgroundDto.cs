using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// Root document for a background editor scene.
/// Contains all canvas objects, the layer stack, and the colour palette.
/// </summary>
/// <param name="Objects">
/// All drawable objects in the scene. Each entry is a concrete subtype of
/// <see cref="BackgroundObjectDto"/>, discriminated by the <c>objectType</c> JSON field.
/// Add new properties directly to the relevant subtype and create a corresponding migration.
/// </param>
/// <param name="Layers">
/// Ordered layer stack. Layers determine render order and group objects for
/// visibility and lock control.
/// </param>
/// <param name="Colors">The colour palette available in the editor.</param>
/// <param name="MapId">Optional reference to the map this background belongs to.</param>
/// <param name="Name">The name of the background.</param>
/// <param name="Id">The id of the background.</param>
[PublicAPI]
public record BackgroundDto(
    IReadOnlyList<BackgroundObjectDto> Objects,
    IReadOnlyList<LayerDto> Layers,
    ColorPaletteDto Colors,
    Guid? MapId, string Name, Guid Id);
