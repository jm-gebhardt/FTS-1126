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
        CreateMap<ActionGroup, Models.ActionGroup.ActionGroup>().ReverseMap();
        CreateMap<Models.ActionGroup.AgvAction, AgvAction>()
            .ForMember(p => p.IsSkipped, p => p.MapFrom(src => src.ShouldSkip))
            .ReverseMap();
    }
}