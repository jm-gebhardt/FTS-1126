using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;

/// <summary>
/// Publishes ActionGroup related Dtos
/// </summary>
public interface IActionGroupPublisher
{
    /// <summary>
    /// Publishes a newly created ActionGroup
    /// </summary>
    public Task SendActionGroupCreatedAsync(ActionGroupReadModel actionGroup);

    /// <summary>
    /// Publishes the change update of ActionGroup
    /// </summary>
    public Task SendActionGroupChangedAsync(ActionGroupReadModel actionGroup);

    /// <summary>
    /// Publishes the last order log to all clients.
    /// </summary>
    /// <param name="correlationId">The action group correlation id.</param>
    /// <param name="logEntry">The last order log entry.</param>
    Task SendLogEntryCreated(Guid correlationId, LogEntry logEntry);
}