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
        CreateMap<FilteredActionGroup, FilteredActionGroupDto>()
            .ForMember(g => g.EstimatedCompletionDate, opt => opt.MapFrom(g => g.StartedDate + g.EstimatedDuration))
            .ForSourceMember(g => g.ShouldEscapeQueue, opt => opt.DoNotValidate())
            .ForSourceMember(g => g.Priority, opt => opt.DoNotValidate());
        CreateMap<FilteredAgvAction, FilteredActionDto>()
            .ForMember(p => p.IsSkipped, p => p.MapFrom(src => src.ShouldSkip))
            .ForMember(m => m.TargetNodeType, opt => opt.MapFrom(src => src.Target.Node == null ? AgvGraph.NodeType.WayPoint : src.ActionType.ToNodeType()))
            .ForMember(m => m.TargetNodeId, opt =>
            {
                opt.MapFrom(s => s.Target.Node!.Name);
                opt.Condition(s => s.Target.Node != null);
            });
        CreateMap<FilteredAgv, FilteredAgvDto>(MemberList.None)
            .IncludeMembers(s => s.AgvStatus);

        CreateMap<GraphNode, string>().ConvertUsing(g => g.Name);

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