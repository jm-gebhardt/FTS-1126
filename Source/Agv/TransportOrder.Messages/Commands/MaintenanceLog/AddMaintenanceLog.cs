using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;

/// <summary>
/// Provides a command to add a maintenance log.
/// </summary>
/// <param name="MaintenanceLog">The new maintenance log.</param>
public record AddMaintenanceLog(Domain.Models.MaintenanceLog.MaintenanceLog MaintenanceLog) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the message (the log id).
    /// </summary>
    public Guid CorrelationId => MaintenanceLog.Id;
}