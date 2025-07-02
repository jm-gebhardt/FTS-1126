using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for AGV Wi-Fi metrics related queries.
/// </summary>
public interface IAgvWifiMetricsRepository : IRepository<AgvWifiMetrics>
{
    /// <summary>
    /// Get the Wi-Fi signal strength [dBm].
    /// </summary>
    /// <param name="gridId">The CorrelationId or the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater Timestamp will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less Timestamp will be considered.</param>
    /// <param name="agvSerialNumbers">The serial numbers of the agvs which metrics shall be included.</param>
    /// /// <returns>A scalar heatmap containing average dBm values per grid cell.</returns>
    Task<ScalarHeatmap> GetWifiSignalStrengthHeatmapAsync(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[] agvSerialNumbers);
}