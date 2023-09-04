using AutoMapper;
using TransportOrder.Messages.Requests.Map;

namespace TransportOrder.Messages.Profiles;

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
        CreateMap<Domain.Models.Map.Map, Map>()
            .ForCtorParam(nameof(Map.RelatedAgvs), opt => 
                opt.MapFrom((src, ctx) => new RelatedMapAgvValueResolver().Resolve(src, null!, null!, ctx)));
    }
}