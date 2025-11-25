using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for grid cell related queries.
/// </summary>
public interface IGridCellRepository : IRepository<GridCell>
{
    /// <summary>
    /// Retrieves all grid cells where the specified coordinates (x, y) fall within the cell's boundaries.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    Task<List<GridCell>> GetOrAddGridCellsForCoordinatesAsync(decimal x, decimal y);
}