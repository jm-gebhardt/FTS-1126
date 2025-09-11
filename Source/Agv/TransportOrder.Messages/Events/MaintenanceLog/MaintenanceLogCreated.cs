using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.MaintenanceLog;

/// <summary>
/// Represents the event raised when a maintenance log for an agv has been created.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="MaintenanceLog">The maintenance log.</param>
public record MaintenanceLogCreated(Guid CorrelationId, Domain.Models.MaintenanceLog.MaintenanceLog MaintenanceLog ) : CorrelatedBy<Guid>;