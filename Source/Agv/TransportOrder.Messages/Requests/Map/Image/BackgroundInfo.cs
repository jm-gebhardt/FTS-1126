using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;

/// <summary>
/// Response containing the metadata of the background.
/// </summary>
/// <param name="CorrelationId">Map Correlation Id</param>
/// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented</param>
/// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="Width">The width of the map [px].</param>
/// <param name="Height">The height of the map [px].</param>
/// <param name="HasCustomBackground">Indicates whether the background is a custom background or base.</param>
public record BackgroundInfo(Guid CorrelationId, double Resolution, double XOffset, double YOffset, int Width, int Height, bool HasCustomBackground)
{
    /// <summary>
    /// AutoMapper
    /// </summary>
    [UsedImplicitly]
    private BackgroundInfo() : this(Guid.Empty, 0, 0, 0, 0, 0, false)
    {
    }
}