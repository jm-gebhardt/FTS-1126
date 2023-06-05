using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;

/// <summary>
/// Profile for Actions
/// </summary>
public class ActionProfile : Profile
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ActionProfile()
    {
        CreateMap<ActionGroup, ActionGroupDto>()
            .ForMember(nameof(ActionGroupDto.Actions), opt => opt.MapFrom(src => src.Actions.OrderBy(a => a.Sequence)));
        CreateMap<AgvAction, AgvActionDto>();
    }
}