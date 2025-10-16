using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.System;

/// <summary>
/// Publishes system related events.
/// </summary>
public interface ISystemPublisher
{
    /// <summary>
    /// Publishes the last system log to all clients.
    /// </summary>
    /// <param name="logEntry">The last system log entry.</param>
    Task SendLogEntryCreated(LogEntry logEntry);
}