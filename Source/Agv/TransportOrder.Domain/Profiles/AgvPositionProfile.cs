using AutoMapper;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>
/// Profile for AgvPosition
/// </summary>
[UsedImplicitly]
public class AgvPositionProfile : Profile
{
    /// <inheritdoc />
    public AgvPositionProfile()
    {
        CreateMap<AgvPositionReceived, AgvPositionStatus>()
            .ForMember(c => c.CreatedDate, c => c.Ignore())
            .ReverseMap();
    }
}