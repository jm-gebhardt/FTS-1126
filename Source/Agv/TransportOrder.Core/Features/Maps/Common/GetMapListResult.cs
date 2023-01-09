using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common
{
    /// <summary>
    /// Defines a result for the GetMapList request
    /// </summary>
    public class GetMapListResult
    {
        /// <summary>
        /// <inheritdoc cref="GetMapImageResult"/>
        /// </summary>
        public List<MapDto> Maps { get; init; } = null!;
    }
}
