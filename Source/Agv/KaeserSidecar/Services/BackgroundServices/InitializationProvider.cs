using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.KaeserSidecar.Messages.JamState;

namespace Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;

/// <summary>
/// Provides initialization for the service.
/// </summary>
[UsedImplicitly]
public class InitializationProvider(ILogger<InitializationProvider> logger, KaeserGraphConfiguration configuration, IBus bus) : BackgroundService
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
        var stations = configuration.ConveyorStationMapping.Values;
        logger.LogInformation("Requesting jam state for stations {Stations}", string.Join(", ", stations));
        IEnumerable<Task> postStateRequests = stations.Select(s => bus.Publish(new RequestJamState(s)));
        await Task.WhenAll(postStateRequests);
    }
}