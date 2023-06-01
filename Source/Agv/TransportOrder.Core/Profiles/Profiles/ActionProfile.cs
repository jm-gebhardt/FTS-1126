using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;

/// <summary>
/// Profile for Actions
/// </summary>
public class ActionProfile: Profile
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ActionProfile()
    {
        CreateMap<ActionGroup, ActionGroupDto>();
        CreateMap<AgvAction, AgvActionDto>();
    }
}