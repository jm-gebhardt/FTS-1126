using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;

/// <summary>
/// Profile for AgvDto
/// </summary>
public class AgvProfile: Profile
{
    /// <inheritdoc />
    public AgvProfile()
    {
        CreateMap<IAgv, AgvDto>()
            .ForCtorParam(nameof(AgvDto.IsDisturbed), opt => opt.MapFrom((src, _) => src.AgvStatus?.Disturbed))
            .ForCtorParam(nameof(AgvDto.IsAutomatic), opt => opt.MapFrom((src, _) => src.AgvStatus?.Automatic))
            .ForCtorParam(nameof(AgvDto.BatteryCharge), opt => opt.MapFrom((src, _) => src.AgvStatus?.BatteryCharge))
            .ForCtorParam(nameof(AgvDto.IsCharging), opt => opt.MapFrom((src, _) => src.AgvStatus?.Charging))
            .ForCtorParam(nameof(AgvDto.BadBatteryLevel), opt => opt.MapFrom((_, _) => AgvStatus.BadBatteryLevel))
            .ForCtorParam(nameof(AgvDto.GoodBatteryLevel), opt => opt.MapFrom((_, _) => AgvStatus.GoodBatteryLevel));
        
        
        CreateMap<AgvData, AgvDataDto>();
        CreateMap<AgvStatus, AgvStatusDto>();
    }
}