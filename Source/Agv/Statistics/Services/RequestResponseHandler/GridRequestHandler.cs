using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Requests;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Statistics.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for grid-related requests.
/// </summary>
[UsedImplicitly]
public class GridRequestHandler :
    IConsumer<GetGrids>,
    IConsumer<GetStayDurations>,
    IConsumer<GetErrorFrequencies>,
    IConsumer<GetFieldViolationFrequencies>,
    IConsumer<GetWifiSignalStrength>

{
    private readonly IStatisticsDb _db;

    /// <inheritdoc cref="GridRequestHandler"/>>
    public GridRequestHandler(IStatisticsDb db)
    {
        _db = db;
    }

    private async Task ValidateHeatmapAsync(ScalarHeatmap heatmap, Guid gridId, Guid mapId)
    {
        if (heatmap.Values.Count == 0)
        {
            await _db.Grids.ThrowIfNotExistAsync(gridId);
            await _db.Maps.ThrowIfNotExistAsync(mapId);
        }
    }

    /// <summary>
    /// Handles the <see cref="GetGrids"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetGrids> context)
    {
        ICollection<Grid> grids = await _db.Grids.GetGridsAsync();
        await context.RespondAsync(new GotGrids(grids.ToList()));
    }

    /// <summary>
    /// Handles the <see cref="GetStayDurations"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetStayDurations> context)
    {
        var gridId = context.Message.GridId;
        var mapId = context.Message.MapId;
        DateTime? fromDate = context.Message.FromDate;
        DateTime? toDate = context.Message.ToDate;
        Guid[] agvCorrelationIds = context.Message.AgvCorrelationIds;

        var heatmap = await _db.GridCellStay.GetStayDurations(gridId, mapId, fromDate, toDate, agvCorrelationIds);
        await ValidateHeatmapAsync(heatmap, gridId, mapId);

        await context.RespondAsync(new GotStayDurations(heatmap));
    }

    /// <summary>
    /// Handles the <see cref="GetErrorFrequencies"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetErrorFrequencies> context)
    {
        var get = context.Message;

        var heatmap = await _db.GridAgvErrors.GetErrorOccurence(
            get.GridId,
            get.MapId,
            get.FromDate,
            get.ToDate,
            get.AgvCorrelationIds,
            get.ErrorLevel,
            get.ErrorCode);
        await ValidateHeatmapAsync(heatmap, get.GridId, get.MapId);

        await context.RespondAsync(new GotStayDurations(heatmap));
    }

    /// <summary>
    /// Handles the <see cref="GetFieldViolationFrequencies"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetFieldViolationFrequencies> context)
    {
        var get = context.Message;

        var heatmap = await _db.GridAgvFieldViolations.GetFieldViolationOccurence(
            get.GridId,
            get.MapId,
            get.FromDate,
            get.ToDate,
            get.AgvCorrelationIds);
        await ValidateHeatmapAsync(heatmap, get.GridId, get.MapId);

        await context.RespondAsync(new GotStayDurations(heatmap));
    }

    /// <summary>
    /// Handles the <see cref="GetWifiSignalStrength"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetWifiSignalStrength> context)
    {
        var get = context.Message;

        var heatmap = await _db.GridAgvWifiMetrics.GetWifiSignalStrengthHeatmapAsync(
            get.GridId,
            get.MapId,
            get.FromDate,
            get.ToDate,
            get.AgvCorrelationIds);
        await ValidateHeatmapAsync(heatmap, get.GridId, get.MapId);

        await context.RespondAsync(new GotWifiSignalStrength(heatmap));
    }
}