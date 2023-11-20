using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for Cargo types
/// </summary>
[UsedImplicitly]
public class CargoProfile : Profile
{
    /// <inheritdoc />
    public CargoProfile()
    {
        CreateMap<Cargo, CargoDto>();
    }
}