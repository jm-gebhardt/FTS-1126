using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for <see cref="Station" />.
/// </summary>
[UsedImplicitly]
public class StationProfile : WebApiProfile
{
    /// <inheritdoc cref="StationProfile" />
    public StationProfile()
    {
        CreateMap<Station, StationDto>()
            .ForMember(x => x.State, x => x.MapFrom(c =>
                c.NodeLock != null ? c.NodeLock.NodeState : NodeLockState.Free
            ));
    }
}