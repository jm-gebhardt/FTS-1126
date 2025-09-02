using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;

/// <summary>
/// Provides a request to get maintenance log entries for given agvs (optionally only for a given type).
/// </summary>
/// <param name="AgvCorrelationIds">The correlation ids of the agvs.</param>
/// <param name="Type">Optionally, the maintenance type. </param>
public record GetMaintenanceLogsForAgvs(ICollection<Guid> AgvCorrelationIds, MaintenanceType? Type) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The message correlation id : agv correlation id.
    /// </summary>
    public Guid CorrelationId => AgvCorrelationIds.Count > 0 ? AgvCorrelationIds.First(): Guid.NewGuid();
}