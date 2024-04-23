using Gebhardt.Acc.Common.Domain.Repositories;
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
    /// <returns></returns>
    Task<ICollection<Guid>> GetAllIdsAsync();
}