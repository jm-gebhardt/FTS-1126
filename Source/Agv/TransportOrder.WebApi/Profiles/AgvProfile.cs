using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvDto
/// </summary>
public class AgvProfile : Profile
{
    /// <inheritdoc />
    public AgvProfile()
    {
        AddGlobalIgnore(nameof(DatedMessage.MessageCreatedDate));
        CreateMap<Messages.MessageContent.Agv, AgvDto>().ReverseMap();
        CreateMap<AgvData, AgvDataDto>().ReverseMap();
        CreateMap<Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.Agv, AgvStateDto>();
        CreateMap<AgvRouteReadModel, AgvRouteDto>();
        CreateMap<AgvState, AgvStateType>();
        CreateMap<AgvBatteryChargeChanged, AgvBatteryStatusDto>()
            .ForMember(c => c.BatteryCharge, o => o.MapFrom(s => s.NewBatteryCharge));
    }
}