using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Messages;
using Gebhardt.Acc.OrderTranslator.Messages.Stations;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using StationConfiguration = Gebhardt.Acc.OrderTranslator.Domain.Models.Station.StationConfiguration;

namespace Gebhardt.Acc.OrderTranslator.Services.CommandHandler;

/// <summary>
/// Handles station related commands.
/// </summary>
[UsedImplicitly]
public class StationCommandHandler(
    IStationRepository stationRepository,
    ILogger<StationCommandHandler> logger,
    StationConfiguration stationConfiguration) :
    IConsumer<CreateOrUpdateStation>,
    IConsumer<ReloadStationConfiguration>
{
    /// <inheritdoc/>>
    public async Task Consume(ConsumeContext<CreateOrUpdateStation> context)
    {
        var station = context.Message.Station;
        station.ResetId();

        await stationRepository.AddOrUpdateAsync(station);
        logger.LogInformation("Station {Name} created or updated", station.Name);
        await context.Publish(new StationConfigured(station.Id, station.AsDict([nameof(Station.Name), nameof(Station.Id)])));
    }

    /// <inheritdoc/>>
    public async Task Consume(ConsumeContext<ReloadStationConfiguration> context)
    {
        logger.LogInformation("Loading station configuration");
        stationConfiguration.Reload();

        logger.LogInformation("Broadcasting station group information");
        await Task.WhenAll(stationConfiguration.Stations.Select(s =>
            context.Publish(new StationConfigurationReceived(s.Name.ToGuid(), new Common.Messages.Graphs.Events.StationConfiguration(s.StationGroup)))));

        var stationNames = stationConfiguration.Stations.Select(s => s.Name).ToList();
        logger.LogInformation("Updating stations {Stations}", string.Join(", ", stationNames));

        await Task.WhenAll(stationConfiguration.Stations.Select(s => context.Publish(new CreateOrUpdateStation(s))));

        logger.LogInformation("Station configuration updated successfully");
    }
}