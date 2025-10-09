using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for filtered models.
/// </summary>
[UsedImplicitly]
public class FilteredProfile : Profile
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public FilteredProfile()
    {
        CreateMap<FilteredActionGroup, FilteredActionGroupDto>();
        CreateMap<FilteredAgvAction, FilteredActionDto>()
            .ForMember(m => m.TargetNodeType, opt => opt.MapFrom(src => src.Target.Node == null ? AgvGraph.NodeType.WayPoint : src.ActionType.ToNodeType()));
        CreateMap<FilteredAgv, FilteredAgvDto>(MemberList.None)
            .IncludeMembers(s => s.AgvStatus);

        CreateMap<AgvStatus, FilteredAgvDto>(MemberList.None)
            .ForMember(nameof(FilteredAgvDto.IsDisturbed), opt => opt.MapFrom(src => src.Disturbed))
            .ForMember(nameof(FilteredAgvDto.IsAutomatic), opt => opt.MapFrom(src => src.Automatic))
            .ForMember(nameof(FilteredAgvDto.IsCharging), opt => opt.MapFrom(src => src.Charging))
            .ForMember(nameof(FilteredAgvDto.LastNodeId), opt => opt.MapFrom(src => src.LastNodeId))
            .ForMember(nameof(FilteredAgvDto.Zones), opt => opt.MapFrom(src => src.ZoneIds))
            .ForMember(nameof(FilteredAgvDto.Stations), opt => opt.MapFrom(src => src.StationIds));

        CreateMap<FilteredActionTarget, FilteredActionTargetDto>();
        CreateMap<FilteredGraphNode, FilteredGraphNodeDto>();
        CreateMap<FilteredAgvPosition, FilteredAgvPositionDto>();
    }
}