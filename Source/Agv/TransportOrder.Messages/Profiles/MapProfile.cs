using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;

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
            .ForMember(m => m.RelatedAgvs, opt =>
                opt.MapFrom(c => c.MapAgvs.Select(x => x.AgvId).ToArray()))
            .ForMember(m => m.BackgroundInfo, opt =>
                opt.MapFrom(c => GetBackground(c)));

        CreateMap<SetBackground, MapBackground>(MemberList.Source)
            .ForCtorParam(nameof(MapBackground.CorrelationId), opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForCtorParam(nameof(MapBackground.Width), opt => opt.MapFrom((_, ctx) => ctx.Items[nameof(MapBackground.Width)]))
            .ForCtorParam(nameof(MapBackground.Height), opt => opt.MapFrom((_, ctx) => ctx.Items[nameof(MapBackground.Height)]))
            .ForCtorParam(nameof(MapBackground.IsCustom), opt => opt.MapFrom(_ => true))
            ;

        CreateMap<MapBackground, BackgroundInfo>()
            .ForMember(dest => dest.HasCustomBackground, opt => opt.MapFrom(src => src.IsCustom));

        CreateMap<(Map, bool), MapChanged>()
            .ConstructUsing((src, ctx) => new MapChanged(
                ctx.Mapper.Map<Requests.Map.Map>(src.Item1), src.Item2
            ))
            .ForMember(dest => dest.Map, opt => opt.Ignore())
            .ForMember(dest => dest.HasBackgroundChanged, opt => opt.Ignore());
    }

    private static MapBackground GetBackground(Map map) => map.CustomBackground ?? map.BaseBackground ?? throw new MapNotFoundException(map.CorrelationId);
}