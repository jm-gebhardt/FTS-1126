using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvWifiMetricsRepository"/>
public class EfAgvWifiMetricsRepository(StatisticsDbContext context) : HeatmapRepositoryBase<AgvWifiMetrics>(context), IAgvWifiMetricsRepository
{
    /// <inheritdoc cref="IAgvWifiMetricsRepository"/>
    public async Task<ScalarHeatmap> GetWifiSignalStrengthHeatmapAsync(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[] agvSerialNumbers)
    {
        IQueryable<AgvWifiMetrics> query = BuildBaseQuery(gridId, mapId, fromDate, toDate, agvSerialNumbers)
            .Where(e => e.BeaconSignalAverage.HasValue);

        var heatmap = await BuildScalarHeatmapAsync(query, gridId, mapId,
            g =>
                // Ref: page 149, "Analoge Schaltungstechniken der Elektronik" By Wilfried Tenten · 2015
                // Convert dBm to mW, average linearly, then convert back to dBm
                (float) (10 * Math.Log10(g.Average(e => Math.Pow(10, e.BeaconSignalAverage!.Value / 10.0))
                )));

        return heatmap;
    }
}