using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.ConveyorTelegramApiServer.Services;

/// <summary>
/// Provides a communication manager
/// </summary>
[UsedImplicitly]
public class FromWePlcCommunicationManager(ILogger<FromWePlcCommunicationManager> logger,
    [FromKeyedServices(ServiceKeyProvider.FromWeStation)]
    Sender<ConveyorTelegram> sender,
    [FromKeyedServices(ServiceKeyProvider.FromWeStation)]
    Server server,
    [FromKeyedServices(ServiceKeyProvider.FromWeStation)] TelegramCommunicationConfiguration configuration) : BackgroundService
{
    private const int ConnectionDelayMs = 10000;

    /// <inheritdoc cref="FromWePlcCommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.UseConnection)
        {
            logger.LogInformation("Connection to server {Server} configured not to be used, not connecting", ServiceKeyProvider.FromWeStation);
            return;
        }
        await Connect(stoppingToken);
        Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(ConnectionDelayMs, stoppingToken);
        }

        await StopAsync();
    }

    private async Task Connect(CancellationToken stoppingToken)
    {
        bool initialized = false;
        while (!initialized)
        {
            logger.LogInformation("Trying to connect to {Server} telegram server", ServiceKeyProvider.FromWeStation);
            initialized = await server.TryInitializeAsync();
            await Task.Delay(ConnectionDelayMs, stoppingToken);
        }
    }

    private void Start()
    {
        logger.LogInformation("Starting receiver for conveyor communication for {Server}", ServiceKeyProvider.FromWeStation);
        sender.Start();
    }

    private async Task StopAsync()
    {
        logger.LogInformation("Stopping sender for conveyor communication for {Server}", ServiceKeyProvider.FromWeStation);
        await sender.StopAsync();
        server.Disconnect();
    }
}