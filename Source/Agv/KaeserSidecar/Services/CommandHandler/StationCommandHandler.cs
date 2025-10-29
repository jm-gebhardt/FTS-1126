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
        var (station, agvCorrelationId) = context.Message;
        if (!configuration.ConveyorStationMapping.ContainsKey(station.Name))
        {
            logger.LogInformation("Ignoring station occupancy for agv {Agv} at non WE station {Station} ", agvCorrelationId, station.Name);
            return;
        }
        logger.LogInformation("{Message}: Adding agv {Agv} to station {Station}", nameof(OccupyStation), agvCorrelationId, station.Name);
        await stationRepository.OccupyStation(station.Id, agvCorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="FreeStationOccupancy"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeStationOccupancy> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var nodeId = context.Message.NodeId;
        var occupancy = await stationRepository.GetOccupancyByAgvCorrelationId(agvCorrelationId);
        // stationId = nodeId
        if (occupancy?.StationId != nodeId)
        {
            logger.LogWarning("AGV {AgvCorrelationId} does not occupy station {StationId} at all, ignoring FreeStationOccupancy command", agvCorrelationId, nodeId);
            return;
        }
        logger.LogInformation("{Message}: Removing agv {Agv} from station: {StationId}", nameof(FreeStationOccupancy), agvCorrelationId, nodeId);

        await stationRepository.FreeStation(occupancy.StationId);
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
        logger.LogInformation("{Message}: Removing agv {Agv} from station: {StationId}", nameof(SolveResourceErrorsReceived), agvCorrelationId, occupancy.StationId);
        // stationId = nodeId
        await context.Publish(new FreeStationOccupancy(occupancy.StationId, agvCorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="StationUnlocked"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<StationUnlocked> context)
    {
        var unlockedStation = context.Message.StationNode;
        if (!configuration.ConveyorStationMapping.TryGetValue(unlockedStation.Name, out string? jamStateStation))
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
        var stationId = context.Message.StationId;

        var occupancy = await stationRepository.GetOccupancyOfStation(stationId);

        if (occupancy.AgvCorrelationId == null)
        {
            return;
        }

        logger.LogInformation("Removing agv {Agv} from station: {StationId}", occupancy.AgvCorrelationId, stationId);

        await stationRepository.FreeStation(stationId);
    }
}