using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvDto
/// </summary>
public class AgvProfile : WebApiProfile
{
    /// <inheritdoc />
    public AgvProfile()
    {
        CreateMap<Messages.MessageContent.Agv, AgvDto>().ReverseMap();
        CreateMap<AgvData, AgvDataDto>().ReverseMap();
        CreateMap<Messages.MessageContent.Agv, AgvStateDto>();
        CreateMap<CurrentAgvRouteReadModel, CurrentRouteDto>();
        CreateMap<CurrentAgvRouteReadModel, CurrentAgvRouteDto>()
            .ForMember(c => c.CurrentRoute, c => c.MapFrom(o => o));
        CreateMap<AgvState, AgvStateType>();
        CreateMap<AgvBatteryStatus, AgvBatteryStatusDto>();
    }
}