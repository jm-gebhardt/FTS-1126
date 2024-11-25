using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for Map
/// </summary>
public class MapProfile : WebApiProfile
{
    /// <summary>
    /// Constructor
    /// </summary>
    public MapProfile()
    {
        CreateMap<Map, MapDto>();
        CreateMap<NodeLock, NodeStateDto>()
            .ForMember(m => m.StationId, opt => opt.Ignore())
            .ForMember(m => m.User, opt => opt.Ignore())
            .ForMember(m => m.LockReason, opt => opt.Ignore())
            .ForMember(m => m.LockDescription, opt => opt.Ignore());
    }
}