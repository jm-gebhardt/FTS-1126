using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for Agv cargo
/// </summary>
[UsedImplicitly]
public class CargoProfile : Profile
{
    /// <inheritdoc />
    public CargoProfile()
    {
        CreateMap<Cargo, Common.Domain.Models.Cargo>().ReverseMap();
    }
}