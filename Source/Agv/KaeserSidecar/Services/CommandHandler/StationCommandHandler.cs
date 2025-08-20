using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Messages.JamState;
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
    IConsumer<FreeStationOccupancy>,
    IConsumer<SolveResourceErrorsReceived>,
    IConsumer<StationUnlocked>,
    IConsumer<UnblockStation>
{
    /// <summary>
    /// Handles the <see cref="OccupyStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<OccupyStation> context)
    {
        (string station, var agvCorrelationId) = context.Message;
        if (!configuration.ConveyorStationMapping.Keys.Contains(station))
        {
            logger.LogInformation("Ignoring station occupancy for agv {Agv} at non WE station {Station} ", agvCorrelationId, station);
            return;
        }
        logger.LogInformation("{Message}: Adding agv {Agv} to station {Station}", nameof(OccupyStation), agvCorrelationId, station);
        await stationRepository.OccupyStation(station, agvCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="FreeStationOccupancy"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeStationOccupancy> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        string station = context.Message.Station;
        var occupancy = await stationRepository.GetOccupancyByAgvCorrelationId(agvCorrelationId);
        if (occupancy?.Station != station)
        {
            logger.LogWarning("AGV {AgvCorrelationId} does not occupy station {Station} at all, ignoring FreeStationOccupancy command", agvCorrelationId, station);
            return;
        }
        logger.LogInformation("{Message}: Removing agv {Agv} from station: {Station}", nameof(FreeStationOccupancy), agvCorrelationId, station);

        await stationRepository.FreeStation(occupancy.Station, agvCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="SolveResourceErrorsReceived"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SolveResourceErrorsReceived> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var occupancy = await stationRepository.GetOccupancyByAgvCorrelationId(agvCorrelationId);

        if (occupancy == null)
        {
            return;
        }
        logger.LogInformation("{Message}: Removing agv {Agv} from station: {Station}", nameof(SolveResourceErrorsReceived), agvCorrelationId, occupancy.Station);
        await context.Publish(new FreeStationOccupancy(occupancy.Station, agvCorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="StationUnlocked"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<StationUnlocked> context)
    {
        string unlockedStation = context.Message.StationNodeId;
        if (!configuration.ConveyorStationMapping.TryGetValue(unlockedStation, out string? jamStateStation))
        {
            return;
        }
        logger.LogInformation("{Message}: Requesting jam state for unlocked station {Station}", nameof(StationUnlocked), jamStateStation);
        await context.Publish(new RequestJamState(jamStateStation));
    }

    /// <summary>
    /// Handles the <see cref="UnblockStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UnblockStation> context)
    {
        string station = context.Message.Station;

        var occupancy = await stationRepository.GetOccupancyOfStation(station);

        if (occupancy.AgvCorrelationId == null)
        {
            return;
        }

        logger.LogInformation("Removing agv {Agv} from station: {Station}", occupancy.AgvCorrelationId, station);

        await stationRepository.FreeStation(station, occupancy.AgvCorrelationId.Value);
    }
}