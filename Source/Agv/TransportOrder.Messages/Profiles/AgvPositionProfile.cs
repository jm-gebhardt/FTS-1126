using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Messages.Events.Agv;

namespace TransportOrder.Messages.Profiles;

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