using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;

/// <summary>
/// Represents a command to persist a log entry.
/// </summary>
/// <param name="Entry">The entry to be persisted.</param>
public record PersistLogEntry(LogEntry Entry) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => Entry.Id;
}