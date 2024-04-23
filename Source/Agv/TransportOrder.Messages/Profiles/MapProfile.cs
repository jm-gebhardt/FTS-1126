using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

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
            .ForMember(m => m.RelatedAgvs, opt => opt.MapFrom(c => c.MapAgvs.Select(x => x.AgvId).ToArray()))
            ;
    }
}