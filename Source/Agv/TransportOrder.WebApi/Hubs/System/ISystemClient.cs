using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.System;

/// <summary>
/// Provides client methods for system-related events.
/// </summary>
[SignalRHub(SystemHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface ISystemClient
{
    /// <summary>
    /// Publishes the last created system log to all clients.
    /// </summary>
    /// <param name="logEntry">The new log entry.</param>
    [SignalRMethod("SystemLogCreated")]
    Task SystemLogCreated(LogEntryCreatedDto logEntry);
}