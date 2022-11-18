using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using MediatR;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries
{
    /// <summary>
    /// Represents the GetMapList request which returns a list of Maps
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public class GetMapListRequest : IRequest<List<MapDto>>
    {
    }
}
