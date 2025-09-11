using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Provides a message content for an agv maintenance core information.
/// </summary>
[PublicAPI]
public record Maintenance
{
    /// <summary>
    /// The type of maintenance.
    /// </summary>
    public MaintenanceType MaintenanceType { get; init; }

    /// <summary>
    /// The date and time the maintenance was executed.
    /// </summary>
    public DateTime ExecutionDate { get; init; }
}