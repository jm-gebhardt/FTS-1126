using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for Map
/// </summary>
public class MapProfile : Profile
{
    /// <summary>
    /// Constructor
    /// </summary>
    public MapProfile()
    {
        AddGlobalIgnore(nameof(DatedMessage.MessageCreatedDate));
        CreateMap<Map, MapDto>();
        CreateMap<NodeLock, NodeStateDto>();
    }
}