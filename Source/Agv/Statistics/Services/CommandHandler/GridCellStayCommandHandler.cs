using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Statistics.Services.CommandHandler;

/// <summary>
/// Provides a handler for GridCell stay-related commands.
/// </summary>
[UsedImplicitly]
public class GridCellStayCommandHandler(IStatisticsDb db) : IConsumer<AddGridCellStay>
{
    /// <summary>
    /// Handles the <see cref="AddGridCellStay"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddGridCellStay> context)
    {
        var message = context.Message;

        db.GridCellStay.Add(message.GridCellStay);
        await db.SaveAsync();
    }
}