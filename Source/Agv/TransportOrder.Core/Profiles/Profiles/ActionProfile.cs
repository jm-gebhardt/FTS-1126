using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

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
        CreateMap<ActionGroupReadModel, ActionGroupDto>();
        CreateMap<AgvActionReadModel, AgvActionDto>();
    }
}