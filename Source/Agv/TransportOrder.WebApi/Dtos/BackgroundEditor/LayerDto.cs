using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// A named layer that groups one or more canvas objects and controls their visibility and editability.
/// </summary>
/// <param name="Id">Stable numeric identifier for the layer.</param>
/// <param name="Name">Human-readable display name.</param>
/// <param name="Visible">When <c>false</c> the layer's objects are hidden in the editor.</param>
/// <param name="Locked">When <c>true</c> the layer's objects cannot be modified in the editor.</param>
/// <param name="ObjectIds">Ordered list of object IDs that belong to this layer.</param>
[PublicAPI]
public record LayerDto(
    int Id,
    string Name,
    bool Visible,
    bool Locked,
    IReadOnlyList<int> ObjectIds);
