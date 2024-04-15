using Gebhardt.StoreWare.Agv.Common.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for agv grid position related queries.
/// </summary>
public interface IGridAgvPositionRepository : IRepository<GridAgvPosition>
{
    /// <summary>
    /// Gets the position frequencies for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId or the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater Timestamp will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less Timestamp will be considered.</param>
    Task<PositionFrequency> GetPositionFrequency(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate);
}