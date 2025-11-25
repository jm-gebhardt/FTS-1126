using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using AgvError = Gebhardt.Acc.Statistics.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for agv grid error related queries.
/// </summary>
public interface IAgvErrorRepository : IHeatmapRepository<AgvError>
{
    /// <summary>
    /// Get the error occurence heatmap data.
    /// </summary>
    /// <param name="gridId">The CorrelationId or the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater Timestamp will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less Timestamp will be considered.</param>
    /// <param name="agvSerialNumbers">The serial numbers of the agvs which error shall be included.</param>
    /// <param name="errorLevel">The error level.</param>
    /// <param name="errorCode">The error code.</param>
    Task<ScalarHeatmap> GetErrorOccurence(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[] agvSerialNumbers, ErrorLevel errorLevel, string? errorCode);
}