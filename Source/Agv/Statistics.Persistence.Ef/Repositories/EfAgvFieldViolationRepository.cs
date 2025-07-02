using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvFieldViolationRepository"/>
public class EfAgvFieldViolationRepository(StatisticsDbContext context) : HeatmapRepositoryBase<AgvFieldViolation>(context), IAgvFieldViolationRepository
{
    /// <inheritdoc cref="IAgvFieldViolationRepository"/>
    public async Task<ScalarHeatmap> GetFieldViolationOccurence(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[] agvSerialNumbers)
    {
        IQueryable<AgvFieldViolation> query = BuildBaseQuery(gridId, mapId, fromDate, toDate, agvSerialNumbers);

        var heatmap = await BuildScalarHeatmapAsync(query, gridId, mapId);

        return heatmap;
    }
}