using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Provides a dto for an agv maintenance log.
/// </summary>
public record AgvMaintenanceLogDto
{
    /// <summary>
    /// The user, who created the log.
    /// </summary>
    public string User { get; init; } = string.Empty;

    /// <summary>
    /// The correlation id of the maintained agv.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The type of maintenance.
    /// </summary>
    public MaintenanceType MaintenanceType { get; init; }

    /// <summary>
    /// The date and time the maintenance was executed.
    /// </summary>
    public DateTime ExecutionDate { get; init; }

    /// <summary>
    /// The date and time the log was created.
    /// </summary>
    public DateTime CreationDate { get; init; }

    /// <summary>
    /// Optional comment on the maintenance.
    /// </summary>
    public string? Comment { get; init; }
}