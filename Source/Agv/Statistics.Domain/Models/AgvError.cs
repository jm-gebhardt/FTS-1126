using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents and agv error on a grid.
/// </summary>
public class AgvError : IGridCellAssigned
{
    /// <summary>
    /// The id of the error.
    /// </summary>
    public Guid ErrorId { get; init; }

    /// <inheritdoc />
    public Guid AgvCorrelationId { get; }

    /// <inheritdoc />
    public Guid GridCellId { get; set; }

    /// <inheritdoc />
    public GridCell GridCell { get; init; } = null!;

    /// <inheritdoc />
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// warning: AGV is ready to drive without human intervention
    /// fatal: Agv is not in running condition
    /// </summary>
    public ErrorLevel ErrorLevel { get; init; }

    /// <summary>
    /// The error code.
    /// </summary>
    public string ErrorCode { get; init; }

    /// <summary>
    /// The type of error.
    /// </summary>
    public string ErrorType { get; init; }

    /// <summary>
    /// The error category.
    /// </summary>
    public ErrorCategory ErrorCategory { get; init; }

    /// <summary>
    /// The amount that this error has happened in an aggregated timeframe.
    /// </summary>
    public int ErrorCount { get; init; } = 1;

    /// <inheritdoc cref="AgvError"/>>.
    public AgvError(Guid gridCellId, Guid agvCorrelationId, DateTime timestamp, ErrorLevel level, string code, string type, ErrorCategory category)
    {
        ErrorId = Guid.NewGuid();
        AgvCorrelationId = agvCorrelationId;
        GridCellId = gridCellId;
        Timestamp = timestamp;
        ErrorLevel = level;
        ErrorCode = code;
        ErrorType = type;
        ErrorCategory = category;
    }

    /// <inheritdoc cref="AgvError"/>>.
    public AgvError()
    {
        ErrorCode = string.Empty;
        ErrorType = string.Empty;
        ErrorId = Guid.NewGuid();
    }
}