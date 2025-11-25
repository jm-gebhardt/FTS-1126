using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Models;
using JetBrains.Annotations;

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
    /// The <see cref="GridCell"/> repository.
    /// </summary>
    [PublicAPI]
    IGridCellRepository GridCell { get; }

    /// <summary>
    /// The <see cref="GridCellStay"/> repository.
    /// </summary>
    IGridCellStayRepository GridCellStay { get; }

    /// <summary>
    /// The <see cref="Models.MapGridCell"/> repository.
    /// </summary>
    IMapGridCellRepository MapGridCell { get; }

    /// <summary>
    /// The <see cref="AgvError"/> repository.
    /// </summary>
    IAgvErrorRepository GridAgvErrors { get; }

    /// <summary>
    /// The <see cref="AgvFieldViolation"/> repository.
    /// </summary>
    IAgvFieldViolationRepository GridAgvFieldViolations { get; }

    /// <summary>
    /// The <see cref="AgvWifiMetrics"/> repository.
    /// </summary>
    IAgvWifiMetricsRepository GridAgvWifiMetrics { get; }
}