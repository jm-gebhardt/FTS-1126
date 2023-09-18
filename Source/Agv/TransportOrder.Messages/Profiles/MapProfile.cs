using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;

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
        CreateMap<Map, Requests.Map.Map>()
            .ForCtorParam(nameof(Requests.Map.Map.RelatedAgvs), opt => 
                opt.MapFrom((src, ctx) => new RelatedMapAgvValueResolver().Resolve(src, null!, null!, ctx)));
    }
}