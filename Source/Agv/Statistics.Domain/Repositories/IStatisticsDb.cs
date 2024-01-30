using Gebhardt.StoreWare.Agv.Common.Persistence;
using Statistics.Domain.Models;

namespace Statistics.Domain.Repositories;

/// <summary>
/// Provides a unit-of-work for the statistics data access.
/// </summary>
public interface IStatisticsDb : IUnitOfWork
{
    /// <summary>
    /// The <see cref="Map"/> repository.
    /// </summary>
    IMapRepository Maps { get; }

    /// <summary>
    /// The <see cref="Grid"/> repository.
    /// </summary>
    IGridRepository Grids { get; }

    /// <summary>
    /// The <see cref="AgvGridPosition"/> repository.
    /// </summary>
    IAgvGridPositionRepository GridPositions { get; }
}