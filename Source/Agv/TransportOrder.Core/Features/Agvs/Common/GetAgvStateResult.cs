using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common
{
    /// <summary>
    /// Defines a result for the GetAgvStatus request
    /// </summary>
    public record GetAgvStateResult(AgvStateDto AgvState);
}
