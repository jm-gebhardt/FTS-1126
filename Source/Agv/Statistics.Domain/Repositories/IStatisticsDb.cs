using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

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
    /// The <see cref="GridAgvPosition"/> repository.
    /// </summary>
    IGridAgvPositionRepository GridAgvPositions { get; }
    
    /// <summary>
    /// The <see cref="Models.MapAgvPositions"/> repository.
    /// </summary>
    IMapAgvPositionsRepository MapAgvPositions { get; }
}