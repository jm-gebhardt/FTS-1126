using Gebhardt.StoreWare.Agv.TransportOrder.Common.Dtos;
using MediatR;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Common.Features.Maps.Requests.Queries
{
    /// <summary>
    /// Represents the GetMapList request which returns a list of Maps
    /// </summary>
    public class GetMapListRequest : IRequest<List<MapDto>>
    {
        /// <summary>
        /// <inheritdoc cref="GetMapListRequest"/>
        /// </summary>
        public GetMapListRequest()
        {
            
        }
    }
}
