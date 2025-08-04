using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// Map Model
/// </summary>
/// <param name="CorrelationId">Map Correlation Id.</param>
/// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented.</param>
/// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="Width">The width of the map [px].</param>
/// <param name="Height">The height of the map [px].</param>
/// <param name="IsCustom">Indicates whether the background is a custom background or base.</param>
/// <param name="HasBackgroundChanged">Indicates whether the background of the map has changed.</param>
[PublicAPI]
public record MapChangedDto(Guid CorrelationId, double Resolution, double XOffset, double YOffset, int Width, int Height, bool IsCustom, bool HasBackgroundChanged) : CorrelatedBy<Guid>
{
    /// <summary>
    /// AutoMapper
    /// </summary>
    private MapChangedDto() : this(Guid.Empty, 0, 0, 0, 0, 0, false, false)
    {
    }
}