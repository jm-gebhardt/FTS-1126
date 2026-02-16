using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using Gebhardt.Acc.StationCommunication.Messages.Stations;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Services.CommandHandler;

/// <summary>
/// Handles station related commands.
/// </summary>
[UsedImplicitly]
public class StationCommandHandler(
    ILogger<StationCommandHandler> logger,
    StationConfiguration configuration) :
    IConsumer<ReloadStationConfiguration>
{
    /// <inheritdoc/>>
    public async Task Consume(ConsumeContext<ReloadStationConfiguration> context)
    {
        logger.LogInformation("Loading station configuration");
        configuration.Reload();

        List<string> stationNames = configuration.Stations.Where(s => s.HasJamState).Select(s => s.Name).ToList();
        logger.LogInformation("Requesting jam state for stations {Stations}", string.Join(", ", stationNames));

        IEnumerable<Task> tasks = stationNames.Select(s => context.Publish(new RequestJamState(s)));

        foreach (var station in configuration.Stations)
        {
            tasks = tasks.Append(context.Publish(new StationConfigured(station.Name.ToGuid(), station.AsDict([nameof(Station.Name)]))));
        }

        await Task.WhenAll(tasks);
        logger.LogInformation("Station configuration loaded successfully");
    }
}