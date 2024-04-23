using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
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
            .ForMember(x => x.NodeId, x => x.MapFrom(c => c.Id));
    }
}