using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.OrderTranslator.Domain.Services;
using Gebhardt.Acc.OrderTranslator.Messages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StationConfiguration = Gebhardt.Acc.OrderTranslator.Domain.Models.Station.StationConfiguration;

namespace Gebhardt.Acc.OrderTranslator.Services.BackgroundServices;

/// <summary>
/// Provides initialization for the service.
/// </summary>
[UsedImplicitly]
public class InitializationProvider(ILogger<InitializationProvider> logger, StationConfiguration configuration, IBus bus, StartupHealthCheck healthCheck) : BackgroundService
{
    private readonly TimeSpan _configLoadingWaitTime = TimeSpan.FromSeconds(5);

    /// <inheritdoc cref="BackgroundService.ExecuteAsync"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BroadcastStationConfig();
    }

    private async Task BroadcastStationConfig()
    {
        while (!configuration.IsLoaded)
        {
            await Task.Delay(_configLoadingWaitTime);
        }
        logger.LogInformation("Broadcasting station group information");

        IEnumerable<Task> publishTasks = configuration.Stations.Select(s => bus.Publish(new StationConfigurationReceived(s.Name.ToGuid(), new Common.Messages.Graphs.Events.StationConfiguration(s.StationGroup))));
        await Task.WhenAll(publishTasks);

        List<string> stationNames = configuration.Stations.Select(s => s.Name).ToList();
       
        logger.LogInformation("Initializing stations {Stations}", string.Join(", ", stationNames));
        IEnumerable<Task> createStationsRequests = configuration.Stations.Select(s => bus.Publish(new CreateStation(s)));
        await Task.WhenAll(createStationsRequests);
        healthCheck.StartupCompleted = true;
    }
}

