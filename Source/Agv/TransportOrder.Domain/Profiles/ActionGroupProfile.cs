using AutoMapper;
using TransportOrder.Domain.Models.ActionGroup;

namespace TransportOrder.Domain.Profiles;

public class ActionGroupProfile : Profile
{
    public ActionGroupProfile()
    {
        CreateMap<Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.ActionGroup, ActionGroup>().ReverseMap();
        CreateMap<Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.AgvAction, AgvAction>().ReverseMap();
    }
}