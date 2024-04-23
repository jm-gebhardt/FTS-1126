using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Statistics.Services.CommandHandler;

/// <summary>
/// Provides handlers for grid-related commands.
/// </summary>
[UsedImplicitly]
public class GridCommandHandler : IConsumer<AddGrid>
{
    private readonly IStatisticsDb _db;

    /// <inheritdoc cref="GridCommandHandler" />
    public GridCommandHandler(IStatisticsDb db)
    {
        _db = db;
    }

    /// <summary>
    /// Handles the <see cref="AddGrid"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddGrid> context)
    {
        (string name, float xStep, float yStep) = context.Message;
        var grid = new Grid(name, 1, 1, 0, xStep, 0, yStep);
        _db.Grids.Add(grid);
        await _db.SaveAsync();
        await context.RespondAsync(new Success());
    }
}