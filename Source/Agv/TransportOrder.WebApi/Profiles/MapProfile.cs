using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;
using TransportOrder.Messages.Requests.Map;

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
        CreateMap<Map, MapDto>();
    }
}