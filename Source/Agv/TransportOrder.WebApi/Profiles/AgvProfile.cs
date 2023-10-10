using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
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
        CreateMap<Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.Agv, AgvDto>().ReverseMap();
        CreateMap<AgvData, AgvDataDto>().ReverseMap();
        CreateMap<AgvRouteReadModel, AgvRouteDto>();
    }
}