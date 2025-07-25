using Gebhardt.Acc.TransportOrder.WebApi.Dtos.RestrictedArea;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.RestrictedArea;

/// <summary>
/// Restricted area related SignalR methods
/// </summary>
[SignalRHub(RestrictedAreaHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface IRestrictedAreaClient
{
    /// <summary>
    /// Restricted area changed.
    /// </summary>
    [SignalRMethod("RestrictedAreaChanged")]
    Task RestrictedAreaChanged(RestrictedAreaDto restrictedArea);

    /// <summary>
    /// Restricted area deleted.
    /// </summary>
    [SignalRMethod("RestrictedAreaDeleted")]
    Task RestrictedAreaDeleted(RestrictedAreaDeletedDto restrictedArea);
}