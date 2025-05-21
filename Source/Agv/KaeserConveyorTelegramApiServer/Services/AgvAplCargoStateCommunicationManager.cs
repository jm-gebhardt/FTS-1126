using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserConveyorTelegramApiServer.Services;

/// <summary>
/// Provides a communication manager
/// </summary>
public class AgvAplCargoStateCommunicationManager(ILogger<AgvAplCargoStateCommunicationManager> logger, [FromKeyedServices(ServiceKeyProvider.AgvCargoState)] Receiver<ConveyorTelegram> receiver, [FromKeyedServices(ServiceKeyProvider.AgvCargoState)] Server server) : BackgroundService
{
    /// <inheritdoc cref="AgvAplCargoStateCommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Connect();
        Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await StopAsync();
    }

    private void Connect()
    {
        server.TryInitialize();
    }

    private void Start()
    {
        logger.LogInformation("Starting sender for conveyor communication with {Server} server", ServiceKeyProvider.AgvCargoState);
        receiver.Start();
    }

    private async Task StopAsync()
    {
        logger.LogInformation("Stopping sender for conveyor communication with {Server} server", ServiceKeyProvider.AgvCargoState);
        await receiver.StopAsync();
        server.Disconnect();
    }
}