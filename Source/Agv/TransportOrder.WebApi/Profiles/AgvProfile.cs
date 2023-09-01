using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvDto
/// </summary>
public class AgvProfile : Profile
{
    /// <inheritdoc />
    public AgvProfile()
    {
        CreateMap<global::TransportOrder.Messages.MessageContent.Agv, AgvDto>();
        CreateMap<AgvData, AgvDataDto>();
    }
}