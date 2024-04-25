using Gebhardt.Acc.Statistics.Domain.Models;
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
    IConsumer<GetPositionFrequencies>,
    IConsumer<GetErrorFrequencies>

{
    private readonly IStatisticsDb _db;

    /// <inheritdoc cref="GridRequestHandler"/>>
    public GridRequestHandler(IStatisticsDb db)
    {
        _db = db;
    }

    /// <summary>
    /// Handles the <see cref="GetGrids"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetGrids> context)
    {
        ICollection<Grid> grids = await _db.Grids.GetAll();
        await context.RespondAsync(new GotGrids(grids.ToList()));
    }

    /// <summary>
    /// Handles the <see cref="GetPositionFrequencies"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetPositionFrequencies> context)
    {
        var gridId = context.Message.GridId;
        var mapId = context.Message.MapId;
        DateTime? fromDate = context.Message.FromDate;
        DateTime? toDate = context.Message.ToDate;

        var positionFrequency = await _db.GridAgvPositions.GetPositionFrequency(gridId, mapId, fromDate, toDate);
        if (positionFrequency.Frequencies.Count == 0)
        {
            await _db.Grids.ThrowIfNotExistAsync(gridId);
            await _db.Maps.ThrowIfNotExistAsync(mapId);
        }

        await context.RespondAsync(new GotPositionFrequencies(positionFrequency));
    }

    /// <summary>
    /// Handles the <see cref="GetErrorFrequencies"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetErrorFrequencies> context)
    {
        (var gridId, var mapId, DateTime? fromDate, DateTime? toDate, string[] agvSerialNumbers, var errorLevel, string? errorCode) = context.Message;

        var errorFrequency = await _db.GridAgvErrors.GetErrorOccurence(gridId, mapId, fromDate, toDate, agvSerialNumbers, errorLevel, errorCode);
        if (errorFrequency.Frequencies.Count == 0)
        {
            await _db.Grids.ThrowIfNotExistAsync(gridId);
            await _db.Maps.ThrowIfNotExistAsync(mapId);
        }

        await context.RespondAsync(new GotPositionFrequencies(errorFrequency));
    }
}