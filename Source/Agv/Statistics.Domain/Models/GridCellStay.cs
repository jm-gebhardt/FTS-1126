namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents a stay of an Agv within a grid cell.
/// </summary>
public class GridCellStay : IGridCellAssigned
{
    /// <summary>
    /// The unique id of the <see cref="GridCellStay"/>.
    /// </summary>
    public Guid StayId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public Guid AgvCorrelationId { get; init; }

    /// <inheritdoc />
    public Guid GridCellId { get; set; }

    /// <inheritdoc />
    public GridCell GridCell { get; init; } = null!;

    /// <inheritdoc />
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Represents the duration of the stay in that grid cell. For storing only.
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// Represents the duration of the stay in that grid cell.
    /// </summary>
    public TimeSpan Duration
    {
        get => TimeSpan.FromSeconds(DurationSeconds);
        init => DurationSeconds = value.TotalSeconds;
    }

    /// <inheritdoc cref="GridCellStay"/>>.
    public GridCellStay(Guid agvCorrelationId, GridCell gridCell, DateTime timestamp, TimeSpan duration)
    {
        GridCellId = gridCell.CellId;
        GridCell = gridCell;
        AgvCorrelationId = agvCorrelationId;
        Timestamp = timestamp;
        Duration = duration;
    }

    /// <inheritdoc cref="GridCellStay"/>>.
    public GridCellStay()
    {
    }
}