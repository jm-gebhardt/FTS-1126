using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos
{
    /// <summary>
    /// Map Model
    /// </summary>
    /// <param name="CorrelationId">Map Correlation Id</param>
    /// <param name="MapId">Map Id</param>
    /// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented</param>
    /// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
    /// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
    /// <param name="RelatedAgvs">List of Agvs using this map</param>
    [JetBrains.Annotations.PublicAPI]
    public record Map(Guid CorrelationId, string MapId, double Resolution, double XOffset, double YOffset, Guid[] RelatedAgvs) : CorrelatedBy<Guid>;
}