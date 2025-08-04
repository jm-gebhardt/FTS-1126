using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// Map Model
/// </summary>
/// <param name="CorrelationId">Map Correlation Id.</param>
/// <param name="MapId">Map Id.</param>
/// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented.</param>
/// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
/// <param name="RelatedAgvs">List of Agvs using this map</param>
/// <param name="Width">The width of the map [px].</param>
/// <param name="Height">The height of the map [px].</param>
/// <param name="IsCustom">Indicates whether the background is a custom background or base.</param>
[PublicAPI]
public record MapDto(Guid CorrelationId, string MapId, double Resolution, double XOffset, double YOffset, Guid[] RelatedAgvs, int Width, int Height, bool IsCustom) : CorrelatedBy<Guid>
{
    /// <summary>
    /// AutoMapper
    /// </summary>
    private MapDto() : this(Guid.Empty, null!, 0, 0, 0, null!, 0, 0, false)
    {
    }
}