using Gebhardt.StoreWare.Agv.Common.Persistence;
using Statistics.Domain.Models;

namespace Statistics.Domain.Repositories;

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