using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries
{
    /// <summary>
    /// GetAgvActionGroupsRequest
    /// </summary>
    /// <param name="States"></param>
    /// <param name="ToDate"></param>
    /// <param name="Limit"></param>
    public record GetAgvActionGroupsRequest(ActionState[] States, DateTime ToDate, int Limit);
}