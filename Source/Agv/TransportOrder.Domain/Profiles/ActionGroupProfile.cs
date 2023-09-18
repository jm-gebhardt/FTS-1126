using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;

public class ActionGroupProfile : Profile
{
    public ActionGroupProfile()
    {
        CreateMap<ActionGroup, Models.ActionGroup.ActionGroup>().ReverseMap();
        CreateMap<AgvAction, Models.ActionGroup.AgvAction>().ReverseMap();
    }
}