using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.CommandHandler;

/// <summary>
/// Handles housekeeping related commands.
/// </summary>
[UsedImplicitly]
public class HousekeepingCommandHandler : IConsumer<HouseKeepAgvPositions>
{
    private readonly IStatisticsDb _db;
    private readonly ILogger<HousekeepingCommandHandler> _logger;

    /// <inheritdoc cref="HousekeepingCommandHandler"/>
    public HousekeepingCommandHandler(IStatisticsDb db, ILogger<HousekeepingCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="HouseKeepAgvPositions"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HouseKeepAgvPositions> context)
    {
        var timestamp = DateTime.UtcNow - TimeSpan.FromDays(30);

        _logger.LogInformation("Housekeeping {name}: Dropping older than {timestamp}", nameof(GridAgvPosition), timestamp);

        await _db.GridAgvPositions.DeleteAgvPositionsOlderThan(timestamp);
        await _db.SaveAsync();
    }
}