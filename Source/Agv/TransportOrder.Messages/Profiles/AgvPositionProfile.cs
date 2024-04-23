using AutoMapper;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for AgvPosition
/// </summary>
public class AgvPositionProfile : Profile
{
    /// <inheritdoc />
    public AgvPositionProfile()
    {
        CreateMap<AgvPositionReceived, AgvPositionStatus>().ReverseMap();
        CreateMap<AgvPositionChanged, AgvPositionStatus>()
            .ForCtorParam(nameof(AgvPositionStatus.AgvPosition),
                opt =>
                    opt.MapFrom(src => src.NewAgvPosition)).ReverseMap();
    }
}