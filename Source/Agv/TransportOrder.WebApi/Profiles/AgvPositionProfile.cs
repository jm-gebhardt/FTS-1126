using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvPosition
/// </summary>
public class AgvPositionProfile : Profile
{
    /// <inheritdoc />
    public AgvPositionProfile()
    {
        CreateMap<AgvPositionStatus, AgvPositionStatusDto>();
        CreateMap<AgvPositionReceived, AgvPositionStatus>();
        CreateMap<AgvPositionChanged, AgvPositionStatus>()
            .ForCtorParam(nameof(AgvPositionStatus.AgvPosition), 
                opt => 
                    opt.MapFrom(src => src.NewAgvPosition));
    }
}