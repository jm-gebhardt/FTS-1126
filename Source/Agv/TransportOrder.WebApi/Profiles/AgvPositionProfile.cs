using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

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
        CreateMap<AgvPositionReceived, AgvPositionStatus>()
            .ForSourceMember(s => s.ForcePublish, opt => opt.DoNotValidate());
        CreateMap<AgvPositionChanged, AgvPositionStatus>()
            .ForCtorParam(nameof(AgvPositionStatus.AgvPosition),
                opt =>
                    opt.MapFrom(src => src.NewAgvPosition));
    }
}