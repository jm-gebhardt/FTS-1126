using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.RestrictedArea;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for restricted areas
/// </summary>
public class RestrictedAreaProfile: WebApiProfile
{
    /// <inheritdoc cref="RestrictedAreaProfile"/>>
    public RestrictedAreaProfile()
    {
        CreateMap<RestrictedArea, RestrictedAreaDto>();
    }
}