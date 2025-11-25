using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents an agv field violation on a grid.
/// </summary>
public class AgvFieldViolation : IGridCellAssigned
{
    /// <summary>
    /// The id of the violation.
    /// </summary>
    public Guid ViolationId { get; init; }

    /// <inheritdoc />
    public Guid AgvCorrelationId { get; }

    /// <inheritdoc />
    public Guid GridCellId { get; set; }

    /// <inheritdoc />
    public GridCell GridCell { get; init; } = null!;

    /// <inheritdoc />
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The amount of times that a violation occured on the position.
    /// </summary>
    [PublicAPI]
    public int ViolationCount { get; } = 1;

    /// <inheritdoc cref="AgvFieldViolation"/>>.
    public AgvFieldViolation(Guid agvCorrelationId, Guid gridCellId, DateTime timestamp)
    {
        ViolationId = Guid.NewGuid();
        AgvCorrelationId = agvCorrelationId;
        GridCellId = gridCellId;
        Timestamp = timestamp;
    }

    /// <inheritdoc cref="AgvFieldViolation"/>>.
    public AgvFieldViolation()
    {
        ViolationId = Guid.NewGuid();
    }
}