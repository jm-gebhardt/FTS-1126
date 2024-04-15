using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvPosition
/// </summary>
public class AgvPositionProfile : WebApiProfile
{
    /// <inheritdoc />
    public AgvPositionProfile()
    {
        CreateMap<AgvPositionStatus, AgvPositionStatusDto>();
        CreateMap<AgvPosition, AgvPositionDto>();
        CreateMap<AgvPositionReceived, AgvPositionStatus>();
        CreateMap<AgvPositionChanged, AgvPositionStatus>()
            .ForCtorParam(nameof(AgvPositionStatus.AgvPosition),
                opt =>
                    opt.MapFrom(src => src.NewAgvPosition));
    }
}