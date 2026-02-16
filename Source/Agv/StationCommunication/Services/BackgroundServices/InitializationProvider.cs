using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Messages.Stations;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Services.BackgroundServices;

/// <summary>
/// Provides initialization for the service.
/// </summary>
[UsedImplicitly]
public class InitializationProvider(
    ILogger<InitializationProvider> logger,
    StationConfiguration configuration,
    IBus bus) : BackgroundService
{
    private readonly TimeSpan _configLoadingWaitTime = TimeSpan.FromSeconds(5);

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ensure lazy singleton is loaded
        _ = configuration.ConfigFilePath;

        await Task.Delay(_configLoadingWaitTime, stoppingToken);

        logger.LogInformation("Requesting initial station configuration load");
        await bus.Publish(new ReloadStationConfiguration(), stoppingToken);
    }
}