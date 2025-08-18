using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map;

/// <summary>
/// Represents the background for a map.
/// </summary>
/// <inheritdoc cref="MapBackground"/>
/// <param name="CorrelationId">Id of the background</param>
/// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented</param>
/// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="Width">The width of the map [px]</param>
/// <param name="Height">The height of the map [px]</param>
/// <param name="Image">Map background in form bytes</param>
/// <param name="IsCustom">Indicates whether the background is a custom background or base.</param>
public record MapBackground(Guid CorrelationId, double Resolution, double XOffset, double YOffset, int Width, int Height, byte[] Image, bool IsCustom = false)
{
    /// <summary>The resolution of the maps image. Expressed as how many [m] per pixel are represented</summary>
    public double Resolution { get; set; } = Resolution;

    /// <summary>The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</summary>
    public double XOffset { get; set; } = XOffset;

    /// <summary>The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</summary>
    public double YOffset { get; set; } = YOffset;

    /// <summary>Indicates whether the background is a custom background or base.</summary>
    public bool IsCustom { get; set; } = IsCustom;

    /// <summary>
    /// EF
    /// </summary>
    [UsedImplicitly]
    private MapBackground() : this(Guid.Empty, 0, 0, 0, 0, 0, Array.Empty<byte>())
    {
    }
}