using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.Station;
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
    IConsumer<RemoveAgvFromStation>,
    IConsumer<AddAgvToStation>
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
    /// Handles the <see cref="RemoveAgvFromStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromStation> context)
    {
        var(stationId, agvCorrelationId) = context.Message;
       
        var station = await stationRepository.GetByIdAsync(stationId);
        if (station.AgvCorrelationId != agvCorrelationId)
        {
            logger.LogWarning("AGV {AgvCorrelationId} does not occupy station: {Station}", agvCorrelationId, station.Node);
            return;
        }

        logger.LogInformation("Removing agv {Agv} from station {Station}", agvCorrelationId, station.Node);
        station.RemoveAgv();

        await stationRepository.SaveAsync();

        await context.Publish(new AgvLeftStation(station.Source, station.Target));
    }

    /// <summary>
    /// Handles the <see cref="AddAgvToStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddAgvToStation> context)
    {
        var (stationId, agvCorrelationId) = context.Message;

        var station = await stationRepository.GetByIdAsync(stationId);
        logger.LogInformation("Adding agv {Agv} to station {Station}", agvCorrelationId, station.Node);
        station.AddAgv(agvCorrelationId);
        await stationRepository.SaveAsync();
    }
}