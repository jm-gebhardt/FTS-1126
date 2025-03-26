using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Messages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.CommandHandler;

/// <summary>
/// Handles station-related commands.
/// </summary>
[UsedImplicitly]
public class StationCommandHandler(IStationRepository stationRepository, ILogger<StationCommandHandler> logger) :
    IConsumer<OccupyStation>,
    IConsumer<FreeStation>
{
    /// <summary>
    /// Handles the <see cref="OccupyStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<OccupyStation> context)
    {
        (string station, var agvCorrelationId) = context.Message;
        logger.LogInformation("Adding agv {Agv} to station {Station}", agvCorrelationId, station);
        await stationRepository.OccupyStation(station, agvCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="FreeStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeStation> context)
    {
        (string station, var agvCorrelationId) = context.Message;
        logger.LogInformation("Removing agv {Agv} from station {Station}", agvCorrelationId, station);
        await stationRepository.FreeStation(station, agvCorrelationId);
    }
}