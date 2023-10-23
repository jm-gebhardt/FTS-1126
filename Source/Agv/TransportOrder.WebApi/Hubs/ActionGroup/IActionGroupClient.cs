using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;

/// <summary>
/// ActionGroup related SignalR methods
/// </summary>
[SignalRHub(ActionGroupHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface IActionGroupClient
{
    /// <summary>
    /// ActionGroup created
    /// </summary>
    /// <param name="actionGroup"></param>
    [SignalRMethod("ActionGroupCreated")]
    Task ActionGroupCreated(ActionGroupDto actionGroup);

    /// <summary>
    /// ActionGroup changed
    /// </summary>
    /// <param name="actionGroup"></param>
    [SignalRMethod("ActionGroupChanged")]
    Task ActionGroupChanged(ActionGroupDto actionGroup);
}