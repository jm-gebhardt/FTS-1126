using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Messages.Requests;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Statistics.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for grid-related requests.
/// </summary>
[UsedImplicitly]
public class GridRequestHandler :
    IConsumer<GetGrids>,
    IConsumer<GetPositionFrequencies>
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

        await context.RespondAsync(new GotPositionFrequencies(positionFrequency));
    }
}