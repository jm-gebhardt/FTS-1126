using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;

/// <summary>
/// Profile for ActionGroup
/// </summary>
public class ActionGroupProfile : Profile
{
    /// <summary/>
    public ActionGroupProfile()
    {
        CreateMap<ActionGroup, Models.ActionGroup.ActionGroup>().ReverseMap();
        CreateMap<AgvAction, Models.ActionGroup.AgvAction>().ReverseMap();
    }
}