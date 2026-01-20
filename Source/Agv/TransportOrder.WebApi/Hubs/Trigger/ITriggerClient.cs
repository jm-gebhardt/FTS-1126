using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;

/// <summary>
/// Trigger related SignalR methods
/// </summary>
[SignalRHub(TriggerHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface ITriggerClient
{
    /// <summary>
    /// Trigger changed
    /// </summary>
    [SignalRMethod("TriggerChanged")]
    Task TriggerChanged(TriggerDto trigger);

    /// <summary>
    /// Trigger deleted
    /// </summary>
    [SignalRMethod("TriggerDeleted")]
    Task TriggerDeleted(Guid triggerId);

    /// <summary>
    /// Trigger restored
    /// </summary>
    [SignalRMethod("TriggerRestored")]
    Task TriggerRestored(Guid triggerId);
}