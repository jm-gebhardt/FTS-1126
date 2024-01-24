using Gebhardt.StoreWare.Agv.Common.Persistence;
using Statistics.Domain.Models;
using Statistics.Domain.ReadModels;

namespace Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for agv grid position related queries.
/// </summary>
public interface IAgvGridPositionRepository: IRepository<AgvGridPosition>
{
    /// <summary>
    /// Gets the position frequencies for the given grid.
    /// </summary>
    /// <param name="gridId">The grid id.</param>
    Task<PositionFrequency> GetPositionFrequencyForGrid(Guid gridId);
}