using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for Cargo types
/// </summary>
[UsedImplicitly]
public class CargoProfile : WebApiProfile
{
    /// <inheritdoc />
    public CargoProfile()
    {
        CreateMap<Cargo, CargoDto>()
            .ForCtorParam("cargoId", opt => opt.MapFrom(src => src.LoadId))
            .ForCtorParam("cargoType", opt => opt.MapFrom(src => src.LoadType))
            .ForCtorParam("cargoPosition", opt => opt.MapFrom(src => src.LoadPosition)
        );
        
        CreateMap<AgvHasCargo, AgvHasCargoDto>();
    }
}