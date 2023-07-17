using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;

/// <summary>
/// Profile for AgvDto
/// </summary>
public class AgvProfile : Profile
{
    /// <inheritdoc />
    public AgvProfile()
    {
        CreateMap<AgvStatus, AgvDto>(MemberList.None)
            .ForMember(nameof(AgvDto.IsDisturbed), opt => opt.MapFrom(src => src.Disturbed))
            .ForMember(nameof(AgvDto.IsAutomatic), opt => opt.MapFrom(src => src.Automatic))
            .ForMember(nameof(AgvDto.IsCharging), opt => opt.MapFrom(src => src.Charging))
            .ForMember(nameof(AgvDto.BadBatteryLevel), opt => opt.MapFrom(src => AgvStatus.BadBatteryLevel))
            .ForMember(nameof(AgvDto.GoodBatteryLevel), opt => opt.MapFrom(src => AgvStatus.GoodBatteryLevel));

        CreateMap<IAgv, AgvDto>(MemberList.None)
            .IncludeMembers(s => s.AgvStatus);

        CreateMap<ActionGroup, AgvDto>(MemberList.None)
            .ForMember(d => d.CurrentActionGroup, opt => opt.MapFrom(s => s))
            // ignore CorrelationId, as it's set from the other mapping already, otherwise this would overwrite the CorrelationId of the AgvDto (Agv) with the CorrelationId of the ActionGroup
            .ForMember(x => x.CorrelationId, opt => opt.Ignore());

        CreateMap<IList<AgvError>, AgvDto>(MemberList.None)
             .ForMember(nameof(AgvDto.Errors), opt => opt.MapFrom(s => s));

        CreateMap<AgvData, AgvDataDto>();
        CreateMap<AgvStatus, AgvStatusDto>();
    }
}