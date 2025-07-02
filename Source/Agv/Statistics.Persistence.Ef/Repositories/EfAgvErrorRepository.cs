using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using AgvError = Gebhardt.Acc.Statistics.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvErrorRepository"/>
public class EfAgvErrorRepository(StatisticsDbContext context) : HeatmapRepositoryBase<AgvError>(context), IAgvErrorRepository
{
    /// <inheritdoc cref="IAgvErrorRepository"/>
    public async Task<ScalarHeatmap> GetErrorOccurence(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[] agvSerialNumbers, ErrorLevel errorLevel, string? errorCode)
    {
        IQueryable<AgvError> query = BuildBaseQuery(gridId, mapId, fromDate, toDate, agvSerialNumbers)
            .Where(e => e.ErrorLevel == errorLevel)
            .WhereIf(e => e.ErrorCode == errorCode, errorCode != null);

        var heatmap = await BuildScalarHeatmapAsync(query, gridId, mapId);

        return heatmap;
    }
}