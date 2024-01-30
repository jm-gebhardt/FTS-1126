using JetBrains.Annotations;
using MassTransit;
using Statistics.Domain.Models;
using Statistics.Domain.Repositories;
using Statistics.Messages.Requests;

namespace Gebhardt.StoreWare.Agv.Statistics.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for grid-related requests.
/// </summary>
[UsedImplicitly]
public class GridRequestHandler :
    IConsumer<GetGrids>,
    IConsumer<GetPositionFrequenciesForGrid>
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
    /// Handles the <see cref="GetPositionFrequenciesForGrid"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetPositionFrequenciesForGrid> context)
    {
        var gridId = context.Message.GridId;

        var positionFrequency = await _db.GridPositions.GetPositionFrequencyForGrid(gridId);

        await context.RespondAsync(new GotPositionFrequenciesForGrid(positionFrequency));
    }
}