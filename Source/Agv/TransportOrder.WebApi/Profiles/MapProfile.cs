using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
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
        CreateMap<Map, MapDto>()
            .IncludeMembers(m => m.BackgroundInfo);

        CreateMap<(Map, bool), MapChangedDto>()
            .IncludeMembers(m => m.Item1.BackgroundInfo)
            .ForMember(dest => dest.HasBackgroundChanged, opt => opt.MapFrom(src => src.Item2))
            ;

        CreateMap<BackgroundInfo, MapDto>(MemberList.Source)
            .ForMember(m => m.IsCustom, opt => opt.MapFrom(src => src.HasCustomBackground));
        CreateMap<BackgroundInfo, MapChangedDto>(MemberList.Source)
            .ForMember(m => m.IsCustom, opt => opt.MapFrom(src => src.HasCustomBackground));

        CreateMap<NodeLock, NodeStateDto>()
            .ConstructUsing(_ => new NodeStateDto())
            .ForMember(m => m.NodeId, opt => opt.MapFrom(src => src.Node.Name))
            .ForMember(m => m.StationId, opt => opt.Ignore())
            .ForMember(m => m.User, opt => opt.Ignore())
            .ForMember(m => m.LockReason, opt => opt.Ignore())
            .ForMember(m => m.LockDescription, opt => opt.Ignore());

        CreateMap<SetBackgroundDto, SetBackground>()
            .ForCtorParam(nameof(SetBackground.CorrelationId), opt => opt.MapFrom((_, ctx) => (Guid) ctx.Items[nameof(SetBackground.CorrelationId)]))
            .ForCtorParam(nameof(SetBackground.Image), opt => opt.MapFrom((_, ctx) => (byte[]) ctx.Items[nameof(SetBackground.Image)]));

        CreateMap<UpdateMapDto, UpdateMap>()
            .ForCtorParam(nameof(UpdateMap.CorrelationId), opt => opt.MapFrom((_, ctx) => (Guid) ctx.Items[nameof(SetBackground.CorrelationId)]));
    }
}