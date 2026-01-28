using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Services.BackgroundServices;

/// <summary>
/// Provides initialization for the service.
/// </summary>
[UsedImplicitly]
public class InitializationProvider(ILogger<InitializationProvider> logger, StationConfiguration configuration, IBus bus) : BackgroundService
{
    private readonly TimeSpan _configLoadingWaitTime = TimeSpan.FromSeconds(5);

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequestJamStates();
    }

    private async Task RequestJamStates()
    {
        while (!configuration.IsLoaded)
        {
            await Task.Delay(_configLoadingWaitTime);
        }
        List<string> stationNames = configuration.Stations.Where(s => s.HasJamState).Select(s => s.Name).ToList();
        logger.LogInformation("Requesting jam state for stations {Stations}", string.Join(", ", stationNames));
        IEnumerable<Task> tasks = stationNames.Select(s => bus.Publish(new RequestJamState(s)));
        foreach (var station in configuration.Stations)
        {
            tasks = tasks.Append(bus.Publish(new StationConfigured(station.Name.ToGuid(), station.AsDict([nameof(Station.Name)]))));
        }
           
        await Task.WhenAll(tasks);
    }
}