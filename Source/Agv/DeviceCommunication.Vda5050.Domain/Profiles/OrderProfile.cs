using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Profiles;

/// <summary>
/// Provides a profile to map to orders.
/// </summary>
[UsedImplicitly]
public class OrderProfile : Profile
{
    /// <inheritdoc/>
    public OrderProfile()
    {
        CreateMap<string, OrientationType>();
        CreateMap<Models.Lif.Trajectory, Trajectory>();
    }
}
