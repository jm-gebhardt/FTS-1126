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
    /// Get all grid ids.
    /// </summary>
    Task<ICollection<Guid>> GetAllIdsAsync();

    /// <summary>
    /// Throw <see cref="GridNotFoundException"/> if the grid does not exist.
    /// </summary>
    /// <param name="gridId">The id of the grid.</param>
    /// <exception cref="GridNotFoundException">Thrown, if the grid does not exist.</exception>>
    Task ThrowIfNotExistAsync(Guid gridId);
}