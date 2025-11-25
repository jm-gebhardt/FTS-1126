using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Defines an entity with a spatial position and timestamp.
/// </summary>
public interface IGridCellAssigned : ITimestamped
{
    /// <summary>
    /// The AGV correlation id.
    /// </summary>
    Guid AgvCorrelationId { get; }

    /// <summary>
    /// The id of the grid cell.
    /// </summary>
    [PublicAPI]
    public Guid GridCellId { get; set; }

    /// <summary>
    /// The agv grid position object.
    /// </summary>
    GridCell GridCell { get; }
}