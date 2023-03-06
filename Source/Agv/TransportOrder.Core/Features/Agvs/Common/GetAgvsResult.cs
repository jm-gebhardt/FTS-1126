using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common
{
    /// <summary>
    /// Defines a result for the GetAgvs request
    /// </summary>
    /// <param name="Agvs">The AgvDto's of the Agvs</param>
    public record GetAgvsResult(List<AgvDto> Agvs);
}
