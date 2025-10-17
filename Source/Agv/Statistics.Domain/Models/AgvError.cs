using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents and agv error on a grid.
/// </summary>
public class AgvError : IPositionedEntity
{
    /// <summary>
    /// The id of the error.
    /// </summary>
    public Guid ErrorId { get; init; }

    /// <summary>
    /// The PositionId of the Agv grid position.
    /// </summary>
    public Guid PositionId { get; init; }

    /// <inheritdoc />
    public GridAgvPosition? Position { get; init; }

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
    public AgvError(Guid positionId, DateTime timestamp, ErrorLevel level, string code, string type, ErrorCategory category)
    {
        ErrorId = Guid.NewGuid();
        PositionId = positionId;
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