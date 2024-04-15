using Gebhardt.StoreWare.Agv.Common.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;

namespace Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;

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