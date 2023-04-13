using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common
{
  
    /// <summary>
    /// Defines a result for the GetAgvActionGroups request
    /// </summary>
    /// <param name="AgvActionGroups"></param>
    public record GetAgvActionGroupsResult(IEnumerable<AgvActionGroupDto> AgvActionGroups);
}