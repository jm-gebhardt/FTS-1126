using Gebhardt.StoreWare.Agv.Common.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;

namespace Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for map related queries.
/// </summary>
public interface IMapRepository : IRepository<Map>
{
    /// <summary>
    /// Get all maps for a given grid.
    /// </summary>
    Task<ICollection<Map>> GetMapsForGridAsync(Guid gridId);
}