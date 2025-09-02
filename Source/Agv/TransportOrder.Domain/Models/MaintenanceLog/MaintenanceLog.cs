using System.Text.Json.Serialization;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;

/// <summary>
/// Represents a maintenance log entry.
/// </summary>
public record MaintenanceLog
{
    /// <summary>
    /// The id of the log.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The user, who created the log.
    /// </summary>
    public string User { get; init; }

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

    /// <inheritdoc cref="MaintenanceLog"/>>
    [JsonConstructor]
    public MaintenanceLog(Guid id, string user, Guid agvCorrelationId, DateTime executionDate, DateTime creationDate, MaintenanceType maintenanceType, string? comment)
    {
        Id = id;
        User = user;
        AgvCorrelationId = agvCorrelationId;
        ExecutionDate = executionDate;
        CreationDate = creationDate;
        Comment = comment;
        MaintenanceType = maintenanceType;
    }

    /// <inheritdoc cref="MaintenanceLog"/>>
    public MaintenanceLog(string user, Guid agvCorrelationId, DateTime executionDate, MaintenanceType maintenanceType, string? comment=null)
    {
        Id = Guid.NewGuid();
        User = user;
        AgvCorrelationId = agvCorrelationId;
        ExecutionDate = executionDate;
        CreationDate = DateTime.UtcNow;
        Comment = comment;
        MaintenanceType = maintenanceType;
    }
}