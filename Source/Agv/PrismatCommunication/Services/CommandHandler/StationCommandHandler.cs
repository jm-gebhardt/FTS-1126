using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;

/// <summary>
/// Handles station related commands.
/// </summary>
[UsedImplicitly]
public class StationCommandHandler(IStationRepository stationRepository, ILogger<StationCommandHandler> logger) :
    IConsumer<AddOrUpdateStationGroup>,
    IConsumer<FreeStation>,
    IConsumer<OccupyStation>
{
    /// <summary>
    /// Handles the <see cref="AddOrUpdateStationGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddOrUpdateStationGroup> context)
    {
        (ICollection<Station> stations, var groupId) = context.Message;

        await stationRepository.AddOrUpdateStationsOfGroup(stations, groupId);

        logger.LogInformation("Added or updated stations of group {StationGroup}", groupId);
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="FreeStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<FreeStation> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;

        var station = await stationRepository.GetStationByOccupyingAgv(agvCorrelationId);
        if (station == null)
        {
            logger.LogWarning("AGV {AgvCorrelationId} does not occupy any station", agvCorrelationId);
            return;
        }

        logger.LogInformation("Removing agv {Agv} from station {Station}", agvCorrelationId, station.Node);
        station.RemoveAgv();

        await stationRepository.SaveAsync();

        await context.Publish(new AgvLeftStation(station.Source, station.Target));
    }

    /// <summary>
    /// Handles the <see cref="OccupyStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<OccupyStation> context)
    {
        (string nodeId, var agvCorrelationId) = context.Message;

        var station = await stationRepository.GetStationByNode(nodeId);
        if (station == null)
        {
            logger.LogWarning("Station {NodeId} unknown", nodeId);
            return;
        }
        logger.LogInformation("Adding agv {Agv} to station {Station}", agvCorrelationId, station.Node);
        station.AddAgv(agvCorrelationId);
        await stationRepository.SaveAsync();
    }
}