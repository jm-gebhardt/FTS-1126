using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

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
        CreateMap<AgvGroupCountByState, AgvGroupCountByStateDto>();
    }
}