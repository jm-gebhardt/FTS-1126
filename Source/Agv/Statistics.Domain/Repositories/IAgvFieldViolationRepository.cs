using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for agv field violation related queries.
/// </summary>
public interface IAgvFieldViolationRepository : IRepository<AgvFieldViolation>
{
    /// <summary>
    /// Get the field violation occurence heatmap data.
    /// </summary>
    /// <param name="gridId">The CorrelationId or the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater Timestamp will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less Timestamp will be considered.</param>
    /// <param name="agvSerialNumbers">The serial numbers of the agvs which error shall be included.</param>
    Task<CountHeatmap> GetFieldViolationOccurence(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[] agvSerialNumbers);
}