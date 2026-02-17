using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>
/// Profile for ActionGroup
/// </summary>
public class ActionProfile : Profile
{
    /// <summary/>
    public ActionProfile()
    {
        CreateMap<ActionGroup, Models.ActionGroup.ActionGroup>()
            .ForMember(a => a.DoNotPause, a => a.Ignore())
            .ForMember(a => a.ShouldEscapeQueue, a => a.Ignore())
            .ForMember(a => a.LogEntries, a => a.Ignore())
            .ForMember(a => a.Duration, a => a.Ignore())
            .ReverseMap();

        CreateMap<AgvAction, Models.ActionGroup.AgvAction>()
            .ForMember(p => p.ShouldSkip, p => p.MapFrom(src => src.IsSkipped))
            .ForMember(p => p.AgvActionNodes, p => p.Ignore());

        CreateMap<Models.ActionGroup.AgvAction, AgvAction>()
            .ForMember(p => p.IsSkipped, p => p.MapFrom(src => src.ShouldSkip));

        CreateMap<ActionTarget, Models.ActionGroup.ActionTarget>();
        CreateMap<Models.ActionGroup.ActionTarget, ActionTarget>();
    }
}