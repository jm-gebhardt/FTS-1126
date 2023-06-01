using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;

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
            .ForMember(nameof(AgvDto.GoodBatteryLevel), opt => opt.MapFrom(src => AgvStatus.GoodBatteryLevel))
            ;

        CreateMap<ActionGroup, ActionGroupDto>();

        CreateMap<IAgv, AgvDto>(MemberList.None)
            .IncludeMembers(s => s.AgvStatus);

        CreateMap<AgvData, AgvDataDto>();
        CreateMap<AgvStatus, AgvStatusDto>();
    }
}