using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common
{
  
    /// <summary>
    /// Defines a result for the GetAgvActionGroupCountsByState request
    /// </summary>
    /// <param name="AgvGroupCountByState"></param>
    public record GetAgvActionGroupCountsByStateResult(IEnumerable<AgvGroupCountByStateDto> AgvGroupCountByState);
}