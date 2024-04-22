using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Statistics.Services.CommandHandler;

/// <summary>
/// Handles housekeeping related commands.
/// </summary>
[UsedImplicitly]
public class HousekeepingCommandHandler : IConsumer<HousekeepAgvPositions>
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
    /// Handles the <see cref="HousekeepAgvPositions"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<HousekeepAgvPositions> context)
    {
        var timestamp = DateTime.UtcNow - TimeSpan.FromDays(30);

        _logger.LogInformation("Housekeeping {name}: Dropping older than {timestamp}", nameof(GridAgvPosition), timestamp);

        await _db.GridAgvPositions.DeleteAgvPositionsOlderThan(timestamp);
        await _db.SaveAsync();
    }
}