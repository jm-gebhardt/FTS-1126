using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for grid related queries.
/// </summary>
public interface IGridRepository : IRepository<Grid>
{
    /// <summary>
    /// Retrieves all grids.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of <see cref="Grid"/>.</returns>
    Task<ICollection<Grid>> GetGridsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Throw <see cref="GridNotFoundException"/> if the grid does not exist.
    /// </summary>
    /// <param name="gridId">The id of the grid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="GridNotFoundException">Thrown, if the grid does not exist.</exception>>
    Task ThrowIfNotExistAsync(Guid gridId, CancellationToken cancellationToken = default);
}