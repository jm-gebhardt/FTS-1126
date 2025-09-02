using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;

/// <summary>
/// Provides a request to get maintenance log entries for an agv (optionally only for a given type).
/// </summary>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
/// <param name="Type">Optionally, the maintenance type. </param>
public record GetMaintenanceLogsForAgv(Guid AgvCorrelationId, MaintenanceType? Type) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The message correlation id : agv correlation id.
    /// </summary>
    public Guid CorrelationId => AgvCorrelationId;
}