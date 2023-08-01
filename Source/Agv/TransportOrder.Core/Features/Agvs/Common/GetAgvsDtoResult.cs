using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common
{
    /// <summary>
    /// Defines a result for the <see cref="GetAgvsDtoRequest"/>
    /// </summary>
    /// <param name="Agvs">The AgvDto's of the Agvs</param>
    public record GetAgvsDtoResult(ICollection<AgvDto> Agvs);
}
