using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;

/// <summary>
/// Provides a request to trigger adding a new maintenance logs entry.
/// </summary>
/// <param name="User">The user who added the log.</param>
/// <param name="AgvCorrelationId">The agv that got maintained.</param>
/// <param name="Type">The type of maintenance.</param>
/// <param name="ExecutionDate">The date and time the maintenance was executed.</param>
/// <param name="Comment">Optional comment.</param>
public record TriggerAddingMaintenanceLog(string User, Guid AgvCorrelationId, MaintenanceType Type, DateTime ExecutionDate, string? Comment = null) : CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlation id of the message: agv correlation id.
    /// </summary>
    public Guid CorrelationId => AgvCorrelationId;
}