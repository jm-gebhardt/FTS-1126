using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvWifiMetricsRepository"/>
public class EfAgvWifiMetricsRepository(StatisticsDbContext context) : HeatmapRepositoryBase<AgvWifiMetrics>(context), IAgvWifiMetricsRepository
{
    /// <inheritdoc cref="IAgvWifiMetricsRepository"/>
    public async Task<ScalarHeatmap> GetWifiSignalStrengthHeatmapAsync(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[] agvSerialNumbers)
    {
        IQueryable<AgvWifiMetrics> query = BuildBaseQuery(gridId, mapId, fromDate, toDate, agvSerialNumbers);

        List<ICollection<float>> values = await query
            .Where(e => e.BeaconSignalAverage.HasValue)
            .GroupBy(e => new { e.Position!.XIndex, e.Position!.YIndex })
            .Select(g => new List<float>
            {
                g.Key.XIndex,
                g.Key.YIndex,
                // Ref: page 149, "Analoge Schaltungstechniken der Elektronik" By Wilfried Tenten · 2015
                // Convert dBm to mW, average linearly, then convert back to dBm
                (float) (10 * Math.Log10(g.Average(e => Math.Pow(10, e.BeaconSignalAverage!.Value / 10.0))
                ))
            } as ICollection<float>)
            .ToListAsync();

        return new ScalarHeatmap(gridId, mapId, values);
    }
}