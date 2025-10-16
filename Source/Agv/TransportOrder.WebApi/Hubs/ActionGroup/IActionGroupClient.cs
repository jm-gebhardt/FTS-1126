using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;

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

    /// <summary>
    /// Publishes the last created order log to all clients.
    /// </summary>
    /// <param name="logEntry">The new log entry.</param>
    [SignalRMethod("OrderLogCreated")]
    Task OrderLogCreated(LogEntryCreatedDto logEntry);
}