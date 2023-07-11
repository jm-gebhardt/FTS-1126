using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Resolvers;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;

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
        CreateMap<IMap, MapDto>()
            .ForCtorParam(nameof(MapDto.RelatedAgvs), opt => opt.MapFrom((src, ctx) => new RelatedMapAgvValueResolver().Resolve(src, null!, null!, ctx)));
    }
}