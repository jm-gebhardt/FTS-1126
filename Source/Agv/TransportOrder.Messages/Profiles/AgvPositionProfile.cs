using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for AgvPosition
/// </summary>
public class AgvPositionProfile : Profile
{
    /// <inheritdoc />
    public AgvPositionProfile()
    {
        CreateMap<AgvPositionReceived, AgvPositionStatus>();
        CreateMap<AgvPositionChanged, AgvPositionStatus>()
            .ForCtorParam(nameof(AgvPositionStatus.AgvPosition), 
                opt => 
                    opt.MapFrom(src => src.NewAgvPosition));
    }
}