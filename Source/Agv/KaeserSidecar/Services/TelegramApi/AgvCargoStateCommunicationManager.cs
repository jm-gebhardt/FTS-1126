using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.TelegramApi;

/// <summary>
/// Provides a communication manager
/// </summary>
[UsedImplicitly]
public class AgvCargoStateCommunicationManager(
    ILogger<AgvCargoStateCommunicationManager> logger,
    [FromKeyedServices(ServiceKeyProvider.AgvCargoState)]
    Sender<ConveyorTelegram> sender,
    [FromKeyedServices(ServiceKeyProvider.AgvCargoState)]
    Client client) : BackgroundService
{
    private const int ConnectionDelayMs = 10000;

    /// <inheritdoc cref="AgvCargoStateCommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Connect(stoppingToken);
        Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await StopAsync();
    }

    private async Task Connect(CancellationToken stoppingToken)
    {
        bool initialized = false;
        while (!initialized)
        {
            logger.LogInformation("Trying to connect to {Server} telegram server", ServiceKeyProvider.AgvCargoState);
            initialized = await client.TryInitializeAsync();
            await Task.Delay(ConnectionDelayMs, stoppingToken);
        }
    }

    private void Start()
    {
        logger.LogInformation("Starting sender for conveyor communication for {Server}", ServiceKeyProvider.AgvCargoState);
        sender.Start();
    }

    private async Task StopAsync()
    {
        logger.LogInformation("Stopping sender for conveyor communication for {Server}", ServiceKeyProvider.AgvCargoState);
        await sender.StopAsync();
        client.Disconnect();
    }
}