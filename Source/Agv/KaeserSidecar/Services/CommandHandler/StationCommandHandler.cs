using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.CommandHandler;

/// <summary>
/// Handles station-related commands.
/// </summary>
[UsedImplicitly]
public class StationCommandHandler(IStationRepository stationRepository, ILogger<StationCommandHandler> logger, KaeserGraphConfiguration configuration) :
    IConsumer<OccupyStation>,
    IConsumer<FreeStation>
{
    /// <summary>
    /// Handles the <see cref="OccupyStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<OccupyStation> context)
    {
        (string station, var agvCorrelationId) = context.Message;
        if (!configuration.ConveyorStationMapping.Keys.Contains(station))
        {
            return;
        }
        logger.LogInformation("Adding agv {Agv} to station {Station}", agvCorrelationId, station);
        await stationRepository.OccupyStation(station, agvCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="FreeStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeStation> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        logger.LogInformation("Removing agv {Agv} from station", agvCorrelationId);
        var occupancy = await stationRepository.GetOccupancyByAgvCorrelationId(agvCorrelationId);
        if (occupancy == null)
        {
            logger.LogWarning("AGV {AgvCorrelationId} does not occupy any station", agvCorrelationId);
            return;
        }
        await stationRepository.FreeStation(occupancy.Station, agvCorrelationId);
    }
}