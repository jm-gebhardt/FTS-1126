using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

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
        CreateMap<AgvSettings, AgvSettingsDto>();
        CreateMap<Messages.MessageContent.Agv, AgvStateDto>();
        CreateMap<CurrentAgvRouteReadModel, CurrentRouteDto>();
        CreateMap<CurrentAgvRouteReadModel, CurrentAgvRouteDto>()
            .ForMember(c => c.CurrentRoute, c => c.MapFrom(o => o));
        CreateMap<AgvState, AgvStateType>();
        CreateMap<AgvBatteryChargeChanged, AgvBatteryStatusDto>()
            .ForMember(c => c.BatteryCharge, o => o.MapFrom(s => s.NewBatteryCharge));
    }
}